using System.Collections.Generic;
using Combat.Arena.Core;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>
    /// The hosting machine's round assembler: opens the round with the players that owe a commit,
    /// accepts commits off the transport, and broadcasts the canonical bundle the moment everyone
    /// alive has locked in. Pure relay authority — it never simulates; every client (the host
    /// included) resolves the identical bundle in its own deterministic sim. Also the R10 check
    /// point: commit envelopes piggyback the previous round's state hash, compared to the host's own.
    /// </summary>
    public class ArenaMatchHost
    {
        private readonly ArenaCommitCollector _collector;
        private readonly IArenaTransport _transport;
        private readonly IGameLogger _logger;
        private readonly List<int> _pendingDeparted = new List<int>();

        private ulong _expectedPreviousHash;
        private bool _active;
        private bool _roundBroadcast;

        /// <summary>Raised when a client's reported state hash disagrees with the host's (R10).</summary>
        public event System.Action<int> DesyncDetected;

        public ArenaMatchHost(ArenaCommitCollector collector, IArenaTransport transport, IGameLogger logger)
        {
            _collector = collector;
            _transport = transport;
            _logger = logger;
        }

        /// <summary>Only the hosting machine activates; joined clients never assemble rounds.</summary>
        public void Activate()
        {
            if (_active)
                return;

            _active = true;
            _transport.CommitReceived += HandleCommit;
            _transport.PlayerDeparted += HandleDeparted;
        }

        public void Deactivate()
        {
            if (!_active)
                return;

            _active = false;
            _transport.CommitReceived -= HandleCommit;
            _transport.PlayerDeparted -= HandleDeparted;
        }

        public void BeginRound(int roundNumber, IReadOnlyList<int> alivePlayerIds)
        {
            _roundBroadcast = false;
            _collector.BeginRound(roundNumber, alivePlayerIds);
        }

        /// <summary>The host sim's own end-of-round hash — the reference every client is compared to.</summary>
        public void SetExpectedHash(ulong hash)
        {
            _expectedPreviousHash = hash;
        }

        private void HandleCommit(ArenaCommitEnvelope envelope)
        {
            if (envelope?.Commit == null)
                return;

            if (envelope.PreviousRoundStateHash != 0
                && _expectedPreviousHash != 0
                && envelope.PreviousRoundStateHash != _expectedPreviousHash)
            {
                _logger.Error(LogCategory.Combat,
                    $"[ArenaMatchHost] LOCKSTEP DESYNC: player {envelope.Commit.PlayerId} reported hash " +
                    $"{envelope.PreviousRoundStateHash:X16}, host expected {_expectedPreviousHash:X16}");
                DesyncDetected?.Invoke(envelope.Commit.PlayerId);
            }

            if (!_collector.TryAccept(envelope.RoundNumber, envelope.Commit))
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaMatchHost] Rejected commit from player {envelope.Commit.PlayerId} " +
                    $"for round {envelope.RoundNumber} (stale, duplicate, or not required)");
                return;
            }

            BroadcastIfComplete();
        }

        private void HandleDeparted(int playerId)
        {
            _pendingDeparted.Add(playerId);
            _collector.RemovePlayer(playerId);

            // A departure can be the last missing lock of the round.
            BroadcastIfComplete();
        }

        private void BroadcastIfComplete()
        {
            if (_roundBroadcast || !_collector.AllCommitted)
                return;

            _roundBroadcast = true;
            var bundle = new ArenaRoundBundle(
                _collector.RoundNumber,
                _collector.CommitsInCanonicalOrder(),
                new List<int>(_pendingDeparted));
            _pendingDeparted.Clear();

            _transport.BroadcastBundle(bundle);
        }
    }
}
