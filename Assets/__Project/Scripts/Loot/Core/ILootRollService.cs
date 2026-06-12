using System.Collections.Generic;
using LevelGeneration;

namespace Loot.Core
{
    /// <summary>
    /// Deterministic loot rolling for the three acquisition paths:
    /// platform discovery, enemy drops, and quest rewards.
    /// </summary>
    public interface ILootRollService
    {
        bool ShouldPlaceLootOnPlatform(LevelTheme theme, int platformIndex);

        IReadOnlyList<LootRollResult> RollPlatformLoot(LootRollContext context);

        /// <summary>
        /// Rolls each slot independently. When the slot list is empty,
        /// falls back to the biome's enemy drop table.
        /// </summary>
        IReadOnlyList<LootRollResult> RollEnemyDrops(IReadOnlyList<LootSlotData> slots, LootRollContext context);

        IReadOnlyList<LootRollResult> RollQuestRewards(LootRollContext context);
    }
}
