using System.Linq;
using Combat.Arena.Core;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>
    /// The authority's half of X1: validates rejoin claims at connection approval (the
    /// <see cref="IArenaRejoinGate"/> over the seat ledger), hands every approved rejoiner the
    /// full match stand-up (setup + loadouts + round-start snapshot + the current round's bundle
    /// when it already broadcast), heals a diverged-but-connected client off
    /// <see cref="ArenaMatchHost.DesyncDetected"/> with a targeted resync, and aggregates the
    /// self-reported endpoints into the migration address book. Active only with the host role;
    /// a promoted migration host activates its own instance.
    /// </summary>
    public class ArenaReconnectHost : IArenaRejoinGate
    {
        private readonly ArenaSeatLedger _seatLedger;
        private readonly ArenaMatchHost _matchHost;
        private readonly ArenaMatchContext _matchContext;
        private readonly ArenaCombatController _controller;
        private readonly IArenaTransport _transport;
        private readonly IArenaSessionControl _session;
        private readonly IGameLogger _logger;

        private readonly System.Collections.Generic.Dictionary<int, string> _endpoints =
            new System.Collections.Generic.Dictionary<int, string>();

        private ArenaRoundBundle _lastBroadcastBundle;
        private bool _active;

        public ArenaReconnectHost(
            ArenaSeatLedger seatLedger,
            ArenaMatchHost matchHost,
            ArenaMatchContext matchContext,
            ArenaCombatController controller,
            IArenaTransport transport,
            IArenaSessionControl session,
            IGameLogger logger)
        {
            _seatLedger = seatLedger;
            _matchHost = matchHost;
            _matchContext = matchContext;
            _controller = controller;
            _transport = transport;
            _session = session;
            _logger = logger;
        }

        public void Activate()
        {
            if (_active)
                return;

            _active = true;
            _session.SetRejoinGate(this);
            _session.ClientConnected += HandleClientConnected;
            _transport.BundleReceived += HandleBundleBroadcast;
            _transport.ResyncAckReceived += HandleResyncAck;
            _transport.EndpointReported += HandleEndpointReported;
            _matchHost.DesyncDetected += HandleDesyncDetected;
        }

        public void Deactivate()
        {
            if (!_active)
                return;

            _active = false;
            _session.SetRejoinGate(null);
            _session.ClientConnected -= HandleClientConnected;
            _transport.BundleReceived -= HandleBundleBroadcast;
            _transport.ResyncAckReceived -= HandleResyncAck;
            _transport.EndpointReported -= HandleEndpointReported;
            _matchHost.DesyncDetected -= HandleDesyncDetected;
        }

        // ---- IArenaRejoinGate (called from connection approval) ----

        public bool TryApproveRejoin(int playerId, ulong token, ulong newClientId)
        {
            if (!_seatLedger.TryBeginRejoin(token, newClientId, playerId))
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaReconnectHost] Rejected rejoin claim for player {playerId} " +
                    "(wrong token, or the seat is not gracing)");
                return false;
            }

            // Future disconnects of the new connection must translate back to this seat.
            _transport.RemapClient(playerId, newClientId);
            return true;
        }

        /// <summary>
        /// The approved rejoiner's connection is up — ship it everything. Assembled atomically on
        /// the host main thread: the snapshot and the retained bundle can never straddle a round.
        /// </summary>
        public void HandleClientConnected(ulong clientId)
        {
            var playerId = ResyncingPlayerOn(clientId);
            if (playerId == 0)
                return;

            var snapshot = _controller.CaptureRoundStartSnapshot();
            var bundle = _lastBroadcastBundle != null
                    && _lastBroadcastBundle.RoundNumber == snapshot.RoundNumber
                ? _lastBroadcastBundle
                : null;

            var package = new ArenaRejoinPackage(
                playerId,
                new ArenaMatchSetup(_matchContext.MatchSeed, _matchContext.Roster),
                _matchContext.LoadoutByPlayerId,
                snapshot,
                _seatLedger.DepartedPlayerIds(),
                bundle);

            _logger.Info(LogCategory.Combat,
                $"[ArenaReconnectHost] Sending rejoin package to player {playerId} " +
                $"(round {snapshot.RoundNumber}, bundle replay: {bundle != null})");
            _transport.SendRejoinPackage(clientId, package);
        }

        private void HandleResyncAck(ArenaResyncAck ack)
        {
            if (!_seatLedger.CompleteRejoin(ack.PlayerId))
                return;

            _matchHost.HandleReconnectAccepted(ack.PlayerId);
            _logger.Info(LogCategory.Combat,
                $"[ArenaReconnectHost] Player {ack.PlayerId} is back in lockstep (round {ack.RoundNumber})");
        }

        /// <summary>
        /// R10 heal: a connected client reported a diverged hash — push it the authoritative
        /// round-start snapshot. Its already-accepted commit for the round stands (the bundle is
        /// deterministic for everyone); the heal converges its BOARD.
        /// </summary>
        private void HandleDesyncDetected(int playerId)
        {
            if (!_seatLedger.TryGetClientId(playerId, out var clientId)
                || clientId == _session.LocalClientId)
            {
                return;
            }

            var snapshot = _controller.CaptureRoundStartSnapshot();
            _logger.Warning(LogCategory.Combat,
                $"[ArenaReconnectHost] Healing diverged player {playerId} with the round-" +
                $"{snapshot.RoundNumber} snapshot");
            _transport.SendResync(clientId, new ArenaResyncCommand(playerId, snapshot));
        }

        private void HandleBundleBroadcast(ArenaRoundBundle bundle)
        {
            _lastBroadcastBundle = bundle;
        }

        private void HandleEndpointReported(ulong clientId, ArenaEndpoint endpoint)
        {
            if (endpoint == null || string.IsNullOrEmpty(endpoint.Address))
                return;

            _endpoints[endpoint.PlayerId] = endpoint.Address;
            _transport.BroadcastAddressBook(new ArenaAddressBook(
                _endpoints.Select(pair => new ArenaEndpoint(pair.Key, pair.Value)).ToList()));
        }

        private int ResyncingPlayerOn(ulong clientId)
        {
            foreach (var slot in _matchContext.Roster)
            {
                if (_seatLedger.ConnectionOf(slot.PlayerId) == ArenaSeatConnection.Resyncing
                    && _seatLedger.TryGetClientId(slot.PlayerId, out var seatClientId)
                    && seatClientId == clientId)
                {
                    return slot.PlayerId;
                }
            }

            return 0;
        }
    }
}
