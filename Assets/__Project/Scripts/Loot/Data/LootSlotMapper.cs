using System;
using System.Collections.Generic;
using Loot.Core;
using Loot.Data.Definitions;

namespace Loot.Data
{
    /// <summary>
    /// Converts authored ArtifactLootSlot entries (enemy data) into plain
    /// LootSlotData snapshots for the domain roll service.
    /// </summary>
    public static class LootSlotMapper
    {
        public static IReadOnlyList<LootSlotData> ToData(IReadOnlyList<ArtifactLootSlot> slots)
        {
            if (slots == null || slots.Count == 0)
            {
                return Array.Empty<LootSlotData>();
            }

            var result = new List<LootSlotData>(slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.ArtifactId))
                {
                    continue;
                }

                result.Add(new LootSlotData(
                    slot.ArtifactId,
                    slot.Probability,
                    slot.QuantityRange.x,
                    slot.QuantityRange.y,
                    slot.IsBonus));
            }

            return result;
        }
    }
}
