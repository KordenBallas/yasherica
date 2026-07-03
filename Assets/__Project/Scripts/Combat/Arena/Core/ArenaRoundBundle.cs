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

        public ArenaRoundBundle(
            int roundNumber,
            IReadOnlyList<ArenaCommit> commits,
            IReadOnlyList<int> departedPlayerIds = null)
        {
            RoundNumber = roundNumber;
            Commits = commits ?? new List<ArenaCommit>();
            DepartedPlayerIds = departedPlayerIds ?? new List<int>();
        }
    }
}
