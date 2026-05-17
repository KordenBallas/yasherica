using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Generation
{
    /// <summary>
    /// Factory for creating reward instances from definitions.
    /// Handles chapter scaling, probability rolls, and batch creation.
    /// Pure C# class with minimal Unity dependencies (only Random and Debug).
    /// </summary>
    public class RewardInstanceFactory : IRewardInstanceFactory
    {
        public RewardInstance Create(
            RewardDefinition definition,
            int chapterNumber,
            float multiplier = 1f,
            RewardCondition condition = RewardCondition.Always)
        {
            if (definition == null)
            {
                Debug.LogWarning("[RewardInstanceFactory] Cannot create reward from null definition");
                return null;
            }

            // Check chapter requirement
            if (definition.MinimumChapter > chapterNumber)
            {
                Debug.LogWarning($"[RewardInstanceFactory] Reward '{definition.RewardId}' requires chapter {definition.MinimumChapter}, current: {chapterNumber}");
                return null;
            }

            return new RewardInstance(definition, chapterNumber, multiplier, condition);
        }

        public IReadOnlyList<RewardInstance> CreateFromSlots(
            IReadOnlyList<TemplateRewardSlot> slots,
            int chapterNumber)
        {
            if (slots == null || slots.Count == 0)
                return Array.Empty<RewardInstance>();

            var results = new List<RewardInstance>();

            foreach (var slot in slots)
            {
                if (slot.RewardDefinition == null)
                {
                    Debug.LogWarning("[RewardInstanceFactory] Slot has null reward definition, skipping");
                    continue;
                }

                // Check chapter requirement on definition
                if (slot.RewardDefinition.MinimumChapter > chapterNumber)
                    continue;

                // Roll for probability
                if (slot.Probability < 1f)
                {
                    float roll = UnityEngine.Random.value;
                    if (roll > slot.Probability)
                        continue;
                }

                var instance = Create(
                    slot.RewardDefinition,
                    chapterNumber,
                    slot.ValueMultiplier,
                    slot.Condition);

                if (instance != null)
                {
                    results.Add(instance);
                }
            }

            return results;
        }

        public IReadOnlyList<RewardInstance> CreateFromSlots(
            IReadOnlyList<TemplateRewardSlot> slots,
            int chapterNumber,
            int seed)
        {
            if (slots == null || slots.Count == 0)
                return Array.Empty<RewardInstance>();

            // Save current random state
            var previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);

            try
            {
                var results = new List<RewardInstance>();

                foreach (var slot in slots)
                {
                    if (slot.RewardDefinition == null)
                    {
                        Debug.LogWarning("[RewardInstanceFactory] Slot has null reward definition, skipping");
                        continue;
                    }

                    // Check chapter requirement on definition
                    if (slot.RewardDefinition.MinimumChapter > chapterNumber)
                        continue;

                    // Roll for probability with seeded random
                    if (slot.Probability < 1f)
                    {
                        float roll = UnityEngine.Random.value;
                        if (roll > slot.Probability)
                            continue;
                    }

                    var instance = CreateWithSeededRandom(
                        slot.RewardDefinition,
                        chapterNumber,
                        slot.ValueMultiplier,
                        slot.Condition);

                    if (instance != null)
                    {
                        results.Add(instance);
                    }
                }

                return results;
            }
            finally
            {
                // Restore previous random state
                UnityEngine.Random.state = previousState;
            }
        }

        public RewardInstance RestoreFromSnapshot(
            RewardInstanceSnapshot snapshot,
            RewardDefinition definition)
        {
            if (snapshot == null || definition == null)
            {
                Debug.LogWarning("[RewardInstanceFactory] Cannot restore from null snapshot or definition");
                return null;
            }

            // Use explicit constructor to restore exact values
            var instance = new RewardInstance(
                definition,
                snapshot.CalculatedValue,
                snapshot.Quantity,
                snapshot.Condition);

            // Restore claimed state
            if (snapshot.IsClaimed)
            {
                instance.Claim();
            }

            return instance;
        }

        private RewardInstance CreateWithSeededRandom(
            RewardDefinition definition,
            int chapterNumber,
            float multiplier,
            RewardCondition condition)
        {
            if (definition == null)
                return null;

            // Same logic as regular Create but using seeded random state
            return new RewardInstance(definition, chapterNumber, multiplier, condition);
        }
    }
}
