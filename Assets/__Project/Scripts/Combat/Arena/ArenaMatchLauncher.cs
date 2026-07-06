using System;
using System.Linq;
using Combat.Arena.Core;
using Combat.Arena.Networking;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>
    /// Host-only match kickoff: seats every connected client (host first, then joiners in
    /// ascending clientId order — player/unit ids 1..N), rolls the match seed, closes the session
    /// to late joins, and broadcasts the setup every client builds the identical world from.
    /// </summary>
    public class ArenaMatchLauncher
    {
        private readonly ArenaSessionService _session;
        private readonly IArenaTransport _transport;
        private readonly ArenaDraftHost _draftHost;
        private readonly IGameLogger _logger;

        public ArenaMatchLauncher(
            ArenaSessionService session,
            IArenaTransport transport,
            ArenaDraftHost draftHost,
            IGameLogger logger)
        {
            _session = session;
            _transport = transport;
            _draftHost = draftHost;
            _logger = logger;
        }

        public void StartMatch()
        {
            if (!_session.IsHost)
            {
                _logger.Warning(LogCategory.Combat, "[ArenaMatchLauncher] Only the host starts the match");
                return;
            }

            var clientIds = _session.ConnectedClientIds.OrderBy(id => id).ToList();
            var roster = clientIds
                .Select((clientId, index) => new ArenaRosterSlot(clientId, index + 1, index + 1))
                .ToList();

            _session.MatchStarted = true;
            var matchSeed = Environment.TickCount;

            _logger.Info(LogCategory.Combat,
                $"[ArenaMatchLauncher] Starting match: {roster.Count} players, seed {matchSeed}");
            _transport.BroadcastMatchSetup(new ArenaMatchSetup(matchSeed, roster));

            // The parts draft opens right behind the setup (P4-5): every networked seat is a
            // human, so no AI ids ride along.
            _draftHost.StartDraft(matchSeed, roster, Array.Empty<int>());
        }
    }
}
