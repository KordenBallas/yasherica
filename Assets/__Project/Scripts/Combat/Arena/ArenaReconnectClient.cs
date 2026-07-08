using Combat.Arena.Core;
using Combat.Arena.Data;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>Where the drop machine currently stands (HUD copy hangs off this).</summary>
    public enum ArenaReconnectPhase
    {
        Idle,
        InMatch,
        RetryingHost,
        ConnectingToCandidate,
        Failed
    }

    /// <summary>
    /// The client's half of X1 — ONE state machine for every way a match connection dies,
    /// because a local disconnect is ambiguous (own blip vs host death): retry the known host
    /// for a window; on failure run the deterministic election over the seat mirror; promote
    /// self (re-host + re-seed + round rollback) or dial the elected candidate with a rejoin
    /// claim; time out into the old terminal "connection lost". A live host makes the retry
    /// succeed, so migration never engages by accident. Driven by a per-frame Tick (Zenject
    /// ITickable in production, manual in tests); pure C#.
    /// </summary>
    public class ArenaReconnectClient
    {
        private readonly IArenaSessionControl _session;
        private readonly IArenaTransport _transport;
        private readonly ArenaCombatController _controller;
        private readonly ArenaSnapshotRestorer _restorer;
        private readonly ArenaMatchContext _matchContext;
        private readonly ArenaSeatStatusMirror _mirror;
        private readonly ArenaSeatLedger _seatLedger;
        private readonly ArenaMatchHost _matchHost;
        private readonly ArenaReconnectHost _reconnectHost;
        private readonly ArenaMatchConfig _config;
        private readonly IArenaReconnectClock _clock;
        private readonly IArenaLocalEndpointSource _endpointSource;
        private readonly IGameLogger _logger;

        private ArenaReconnectPhase _phase = ArenaReconnectPhase.Idle;
        private float _windowStartedAt;
        private float _lastAttemptAt;
        private string _dialAddress;
        private bool _armed;

        public ArenaReconnectPhase Phase => _phase;

        public event System.Action<ArenaReconnectPhase, string> PhaseChanged;
        public event System.Action Reconnected;
        public event System.Action ReconnectFailed;

        public ArenaReconnectClient(
            IArenaSessionControl session,
            IArenaTransport transport,
            ArenaCombatController controller,
            ArenaSnapshotRestorer restorer,
            ArenaMatchContext matchContext,
            ArenaSeatStatusMirror mirror,
            ArenaSeatLedger seatLedger,
            ArenaMatchHost matchHost,
            ArenaReconnectHost reconnectHost,
            ArenaMatchConfig config,
            IArenaReconnectClock clock,
            IArenaLocalEndpointSource endpointSource,
            IGameLogger logger)
        {
            _session = session;
            _transport = transport;
            _controller = controller;
            _restorer = restorer;
            _matchContext = matchContext;
            _mirror = mirror;
            _seatLedger = seatLedger;
            _matchHost = matchHost;
            _reconnectHost = reconnectHost;
            _config = config;
            _clock = clock;
            _endpointSource = endpointSource;
            _logger = logger;
        }

        /// <summary>
        /// Combat is live — start watching the session. Also files this seat's endpoint into the
        /// migration book and mirrors the roster.
        /// </summary>
        public void ArmForMatch()
        {
            _armed = true;
            _phase = ArenaReconnectPhase.InMatch;
            _mirror.Reset(_matchContext.Roster);

            _session.ClientDisconnected += HandleClientDisconnected;
            _transport.BundleReceived += HandleBundle;
            _transport.AddressBookReceived += HandleAddressBook;
            _transport.RejoinPackageReceived += HandleRejoinPackage;
            _transport.ResyncReceived += HandleResync;
            _controller.OnGameEnded += HandleGameEnded;

            var address = _endpointSource.GetLocalAddress();
            if (!string.IsNullOrEmpty(address))
            {
                _transport.SubmitEndpoint(new ArenaEndpoint(_matchContext.LocalPlayerId, address));
            }
        }

        public void Disarm()
        {
            if (!_armed)
                return;

            _armed = false;
            _phase = ArenaReconnectPhase.Idle;
            _session.ClientDisconnected -= HandleClientDisconnected;
            _transport.BundleReceived -= HandleBundle;
            _transport.AddressBookReceived -= HandleAddressBook;
            _transport.RejoinPackageReceived -= HandleRejoinPackage;
            _transport.ResyncReceived -= HandleResync;
            _controller.OnGameEnded -= HandleGameEnded;
        }

        /// <summary>Per-frame pump: retry windows and timeouts live here.</summary>
        public void Tick()
        {
            switch (_phase)
            {
                case ArenaReconnectPhase.RetryingHost:
                    if (_clock.Now - _windowStartedAt > _config.ReconnectAttemptSeconds)
                    {
                        RunElection();
                    }
                    else
                    {
                        AttemptOnInterval();
                    }

                    break;

                case ArenaReconnectPhase.ConnectingToCandidate:
                    if (_clock.Now - _windowStartedAt > _config.MigrationConnectTimeoutSeconds)
                    {
                        Fail("the elected host never answered");
                    }
                    else
                    {
                        AttemptOnInterval();
                    }

                    break;
            }
        }

        // ---- drop detection ----

        private void HandleClientDisconnected(ulong clientId)
        {
            // Only the LOCAL connection dying matters here (the host translates peer drops into
            // departures); our own deliberate Shutdown() during retries re-fires this too, so
            // only an InMatch drop starts the machine.
            if (!_armed || _phase != ArenaReconnectPhase.InMatch)
                return;
            if (_session.IsHost || clientId != _session.LocalClientId)
                return;

            _logger.Warning(LogCategory.Combat,
                "[ArenaReconnectClient] Connection lost — retrying the host");
            _dialAddress = _matchContext.HostAddress;
            _windowStartedAt = _clock.Now;
            _lastAttemptAt = float.MinValue;
            SetPhase(ArenaReconnectPhase.RetryingHost, "Connection lost — reconnecting…");
        }

        private void AttemptOnInterval()
        {
            if (_clock.Now - _lastAttemptAt < _config.ReconnectRetryIntervalSeconds)
                return;

            _lastAttemptAt = _clock.Now;
            if (string.IsNullOrEmpty(_dialAddress))
            {
                Fail("no address to dial");
                return;
            }

            _session.Shutdown();
            var payload = ArenaConnectPayload.EncodeRejoin(
                _matchContext.LocalPlayerId,
                ArenaRejoinToken.For(_matchContext.MatchSeed, _matchContext.LocalPlayerId));
            _session.StartClient(_dialAddress, payload);
        }

        // ---- election / migration ----

        private void RunElection()
        {
            var elected = ArenaHostElection.Elect(_mirror.ElectionCandidates(_mirror.HostPlayerId));
            if (elected == 0)
            {
                Fail("nobody left to host");
                return;
            }

            if (elected == _matchContext.LocalPlayerId)
            {
                PromoteSelf();
                return;
            }

            if (!_mirror.TryGetAddress(elected, out var address))
            {
                Fail($"no known address for the elected player {elected}");
                return;
            }

            _logger.Info(LogCategory.Combat,
                $"[ArenaReconnectClient] Host lost — player {elected} elected, dialing {address}");
            _dialAddress = address;
            _windowStartedAt = _clock.Now;
            _lastAttemptAt = float.MinValue;
            SetPhase(ArenaReconnectPhase.ConnectingToCandidate,
                $"Host lost — connecting to player {elected}…");
        }

        private void PromoteSelf()
        {
            _logger.Info(LogCategory.Combat,
                "[ArenaReconnectClient] Host lost — this machine won the election, re-hosting");
            _session.Shutdown();
            if (!_session.StartHost())
            {
                Fail("re-hosting failed");
                return;
            }

            _session.MatchStarted = true;
            _mirror.SetHost(_matchContext.LocalPlayerId);

            // Every other seat starts in grace (they will dial in as rejoiners) — including the
            // dead host's, which may return like anyone else. Already-departed seats stay gone.
            var others = new System.Collections.Generic.List<int>();
            var departed = new System.Collections.Generic.List<int>();
            foreach (var slot in _matchContext.Roster)
            {
                if (slot.PlayerId == _matchContext.LocalPlayerId)
                    continue;

                if (_mirror.LivenessOf(slot.PlayerId) == ArenaSeatLiveness.Departed)
                    departed.Add(slot.PlayerId);
                else
                    others.Add(slot.PlayerId);
            }

            _matchHost.SeedSeats(
                _matchContext.MatchSeed, _matchContext.Roster, _config.DisconnectGraceRounds, others);
            foreach (var playerId in departed)
            {
                _seatLedger.MarkDeparted(playerId);
            }

            _controller.PromoteToHost();
            _reconnectHost.Activate();

            // In-flight commits died with the old host: everyone re-plans the current round on
            // the identical round-start anchor (lockstep made every retained copy the same).
            _controller.ReopenCurrentRound();

            SetPhase(ArenaReconnectPhase.InMatch, "You are the new host");
            Reconnected?.Invoke();
        }

        // ---- state transfer (rejoin package / desync heal) ----

        private void HandleRejoinPackage(ArenaRejoinPackage package)
        {
            if (package.TargetPlayerId != _matchContext.LocalPlayerId)
                return;
            if (_phase != ArenaReconnectPhase.RetryingHost
                && _phase != ArenaReconnectPhase.ConnectingToCandidate)
            {
                return;
            }

            var restored = _restorer.Restore(_controller.CombatState, package.Snapshot);
            if (restored == null)
            {
                Fail("the transferred state could not be restored");
                return;
            }

            _controller.AdoptState(restored, package.Snapshot.LastRoundHash);
            _controller.ReopenCurrentRound();
            if (package.CurrentRoundBundleOrNull != null)
            {
                _controller.ReplayBundle(package.CurrentRoundBundleOrNull);
            }

            _transport.SubmitResyncAck(new ArenaResyncAck(
                _matchContext.LocalPlayerId, package.Snapshot.RoundNumber));

            _logger.Info(LogCategory.Combat,
                $"[ArenaReconnectClient] Rejoined at round {package.Snapshot.RoundNumber}");
            SetPhase(ArenaReconnectPhase.InMatch, "Reconnected");
            Reconnected?.Invoke();
        }

        private void HandleResync(ArenaResyncCommand command)
        {
            if (command.TargetPlayerId != _matchContext.LocalPlayerId
                || _phase != ArenaReconnectPhase.InMatch)
            {
                return;
            }

            if (command.Snapshot.RoundNumber < _controller.CombatState.TurnNumber)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaReconnectClient] Ignoring stale resync for round {command.Snapshot.RoundNumber} " +
                    $"(local round {_controller.CombatState.TurnNumber})");
                return;
            }

            var restored = _restorer.Restore(_controller.CombatState, command.Snapshot);
            if (restored == null)
            {
                _logger.Error(LogCategory.Combat,
                    "[ArenaReconnectClient] Resync restore failed — the sim stays diverged");
                return;
            }

            _logger.Warning(LogCategory.Combat,
                $"[ArenaReconnectClient] LOCKSTEP HEAL: adopting the host's round-" +
                $"{command.Snapshot.RoundNumber} state over the diverged local sim");
            _controller.AdoptState(restored, command.Snapshot.LastRoundHash);
            _controller.ReopenCurrentRound();
            _transport.SubmitResyncAck(new ArenaResyncAck(
                _matchContext.LocalPlayerId, command.Snapshot.RoundNumber));
        }

        // ---- feed the mirror ----

        private void HandleBundle(ArenaRoundBundle bundle)
        {
            _mirror.ApplyBundle(bundle);
        }

        private void HandleAddressBook(ArenaAddressBook book)
        {
            _mirror.ApplyAddressBook(book);
        }

        private void HandleGameEnded(Combat.Core.IPlayer winner, Combat.Core.CombatPhase phase)
        {
            // A finished match must never resurrect itself.
            Disarm();
        }

        private void Fail(string reason)
        {
            _logger.Error(LogCategory.Combat, $"[ArenaReconnectClient] Reconnect failed: {reason}");
            SetPhase(ArenaReconnectPhase.Failed, "Connection to the match was lost");
            ReconnectFailed?.Invoke();
        }

        private void SetPhase(ArenaReconnectPhase phase, string status)
        {
            _phase = phase;
            PhaseChanged?.Invoke(phase, status);
        }
    }
}
