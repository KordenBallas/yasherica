using System.Collections.Generic;
using System.Linq;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Default resolution order (R1): round N starts at index (N−1) mod aliveCount of the
    /// PlayerId-sorted commit list and cycles — initiative rotates every round so nobody holds
    /// it permanently. Deterministic without any PRNG. A seeded-shuffle strategy is the drop-in
    /// alternative (ROADMAP).
    /// </summary>
    public class RotatingInitiativeOrder : IArenaResolutionOrder
    {
        public IReadOnlyList<EnemyIntent> Order(ArenaRoundBundle bundle)
        {
            var byPlayer = bundle.Commits.OrderBy(c => c.PlayerId).ToList();
            var intents = new List<EnemyIntent>();
            if (byPlayer.Count == 0)
                return intents;

            int start = (bundle.RoundNumber - 1) % byPlayer.Count;
            if (start < 0)
                start = 0;

            for (int i = 0; i < byPlayer.Count; i++)
            {
                var commit = byPlayer[(start + i) % byPlayer.Count];
                intents.AddRange(commit.Steps);
            }

            return intents;
        }
    }
}
