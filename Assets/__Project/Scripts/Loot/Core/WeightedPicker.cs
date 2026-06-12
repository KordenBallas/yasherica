using System;
using System.Collections.Generic;

namespace Loot.Core
{
    /// <summary>
    /// Cumulative-weight random selection over loot entries.
    /// Stateless; the caller owns the Random instance for determinism.
    /// </summary>
    public static class WeightedPicker
    {
        public static LootEntryData Pick(
            IReadOnlyList<LootEntryData> entries,
            Func<LootEntryData, float> weightOf,
            Random random)
        {
            if (entries == null || entries.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                var weight = weightOf(entries[i]);
                if (weight > 0f)
                {
                    totalWeight += weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            var roll = (float)(random.NextDouble() * totalWeight);
            float cumulative = 0f;
            LootEntryData lastPositive = null;

            for (int i = 0; i < entries.Count; i++)
            {
                var weight = weightOf(entries[i]);
                if (weight <= 0f)
                {
                    continue;
                }

                lastPositive = entries[i];
                cumulative += weight;
                if (roll < cumulative)
                {
                    return entries[i];
                }
            }

            // Floating-point accumulation can leave the roll a hair past the total.
            return lastPositive;
        }
    }
}
