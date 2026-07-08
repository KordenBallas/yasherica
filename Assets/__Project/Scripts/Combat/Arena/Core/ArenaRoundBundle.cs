using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The host-assembled round: every alive player's commit in canonical (ascending PlayerId)
    /// order plus the players that left since the last round. Broadcast once all alive players
    /// locked in; every client resolves the same bundle deterministically.
    /// </summary>
    public class ArenaRoundBundle
    {
        public int RoundNumber { get; }
        public IReadOnlyList<ArenaCommit> Commits { get; }
        public IReadOnlyList<int> DepartedPlayerIds { get; }

        /// <summary>
        /// Players whose round the authority passed for them (disconnect grace / substituted
        /// commit). Their units did nothing but stay alive — presentation only; normalization
        /// already treats "absent from <see cref="Commits"/>" as a no-op.
        /// </summary>
        public IReadOnlyList<int> AutoPassedPlayerIds { get; }

        public ArenaRoundBundle(
            int roundNumber,
            IReadOnlyList<ArenaCommit> commits,
            IReadOnlyList<int> departedPlayerIds = null,
            IReadOnlyList<int> autoPassedPlayerIds = null)
        {
            RoundNumber = roundNumber;
            Commits = commits ?? new List<ArenaCommit>();
            DepartedPlayerIds = departedPlayerIds ?? new List<int>();
            AutoPassedPlayerIds = autoPassedPlayerIds ?? new List<int>();
        }
    }
}
