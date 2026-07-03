using System;

namespace Combat.Arena.Core
{
    /// <summary>
    /// In-process transport: host and client are the same machine, messages are delivered
    /// synchronously. Used by the offline arena (vs seeded AI dummies) and by tests — the
    /// networked NGO transport implements the same seam, so the round flow above it is identical.
    /// </summary>
    public class LoopbackArenaTransport : IArenaTransport
    {
        public event Action<ArenaCommitEnvelope> CommitReceived;
        public event Action<ArenaMatchSetup> MatchSetupReceived;
        public event Action<ArenaRoundBundle> BundleReceived;
        public event Action<int> PlayerDeparted;

        /// <summary>In-process departures only happen when a test (or tool) injects one.</summary>
        public void SimulateDeparture(int playerId)
        {
            PlayerDeparted?.Invoke(playerId);
        }

        public void SubmitCommit(ArenaCommitEnvelope envelope)
        {
            CommitReceived?.Invoke(envelope);
        }

        public void BroadcastMatchSetup(ArenaMatchSetup setup)
        {
            MatchSetupReceived?.Invoke(setup);
        }

        public void BroadcastBundle(ArenaRoundBundle bundle)
        {
            BundleReceived?.Invoke(bundle);
        }
    }
}
