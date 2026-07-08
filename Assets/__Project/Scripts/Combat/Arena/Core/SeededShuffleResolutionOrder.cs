using System.Collections.Generic;
using System.Linq;
using Loot.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The P4-3 alternative resolution order: a per-round Fisher–Yates shuffle of the
    /// PlayerId-sorted commits, seeded from the match seed + round number — unpredictable to
    /// players (rotation can be planned around; a shuffle cannot) yet identical on every client.
    /// Within a commit, steps keep committed order. Config-selected
    /// (<c>ArenaResolutionOrderMode.SeededShuffle</c>); the default stays rotation.
    /// </summary>
    public class SeededShuffleResolutionOrder : IArenaResolutionOrder
    {
        private readonly ArenaMatchContext _matchContext;

        public SeededShuffleResolutionOrder(ArenaMatchContext matchContext)
        {
            _matchContext = matchContext;
        }

        public IReadOnlyList<Combat.Core.EnemyIntent> Order(ArenaRoundBundle bundle)
        {
            var byPlayer = bundle.Commits.OrderBy(c => c.PlayerId).ToList();
            var intents = new List<Combat.Core.EnemyIntent>();
            if (byPlayer.Count == 0)
                return intents;

            var rng = new System.Random(
                LootSeed.Derive(_matchContext.MatchSeed, $"arena-resolve:{bundle.RoundNumber}"));
            for (int i = byPlayer.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (byPlayer[i], byPlayer[j]) = (byPlayer[j], byPlayer[i]);
            }

            foreach (var commit in byPlayer)
            {
                intents.AddRange(commit.Steps);
            }

            return intents;
        }
    }
}
