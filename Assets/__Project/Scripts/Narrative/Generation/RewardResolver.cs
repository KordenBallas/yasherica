using System;
using System.Collections.Generic;
using CharacterProgression.Core;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Resolves rewards from story RewardSlots using probability rolls.
    /// Pure C# - uses injectable random for testability.
    ///
    /// When a run progression record and condition evaluator are supplied, a
    /// slot whose <see cref="RewardSlot.Condition"/> does not hold against the
    /// current run state is skipped before its probability roll. When they are
    /// null, condition gating is disabled (all slots pass).
    /// </summary>
    public class RewardResolver : IRewardResolver
    {
        private readonly Random _random;
        private readonly IRunProgressionRecord _record;
        private readonly RunConditionEvaluator _conditionEvaluator;

        public RewardResolver() : this(new Random()) { }

        public RewardResolver(Random random) : this(random, null, null) { }

        public RewardResolver(
            Random random,
            IRunProgressionRecord record,
            RunConditionEvaluator conditionEvaluator)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _record = record;
            _conditionEvaluator = conditionEvaluator;
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

                // Run-state gating: a slot whose condition does not hold is skipped.
                if (!PassesCondition(slot))
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

        private bool PassesCondition(RewardSlot slot)
        {
            if (_conditionEvaluator == null || _record == null)
                return true;

            return _conditionEvaluator.Evaluate(slot.Condition, _record);
        }

        private int RandomQuantity(UnityEngine.Vector2Int range)
        {
            if (range.x >= range.y)
                return range.x;

            return _random.Next(range.x, range.y + 1);
        }
    }
}
