using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Resolves rewards from story RewardSlots using probability rolls.
    /// Pure C# - uses injectable random for testability.
    /// </summary>
    public class RewardResolver : IRewardResolver
    {
        private readonly Random _random;

        public RewardResolver() : this(new Random()) { }

        public RewardResolver(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public IReadOnlyList<ResolvedReward> Resolve(StoryDefinition story)
        {
            if (story?.Rewards == null || story.Rewards.Count == 0)
                return Array.Empty<ResolvedReward>();

            var results = new List<ResolvedReward>();

            for (int i = 0; i < story.Rewards.Count; i++)
            {
                var slot = story.Rewards[i];
                if (slot.Reward == null)
                    continue;

                // Probability roll
                var roll = _random.NextDouble();
                if (roll > slot.Probability)
                    continue;

                var quantity = RandomQuantity(slot.Reward.QuantityRange);
                results.Add(new ResolvedReward(slot.Reward, quantity));
            }

            return results;
        }

        private int RandomQuantity(UnityEngine.Vector2Int range)
        {
            if (range.x >= range.y)
                return range.x;

            return _random.Next(range.x, range.y + 1);
        }
    }
}
