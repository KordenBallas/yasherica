using System;
using System.Collections.Generic;
using LevelGeneration;

namespace Loot.Core
{
    /// <summary>
    /// Plain-data snapshot of one biome's loot tables, converted from the
    /// authoring ScriptableObject at install time so the domain stays Unity-free.
    /// </summary>
    public class BiomeLootData
    {
        public LevelTheme Theme { get; }

        public float PlatformLootChance { get; }
        public int PlatformLootCountMin { get; }
        public int PlatformLootCountMax { get; }
        public IReadOnlyList<LootEntryData> PlatformTable { get; }

        public float EnemyDropChance { get; }
        public IReadOnlyList<LootEntryData> EnemyDropTable { get; }

        public int QuestRewardCountMin { get; }
        public int QuestRewardCountMax { get; }
        public IReadOnlyList<LootEntryData> QuestTable { get; }

        public BiomeLootData(
            LevelTheme theme,
            float platformLootChance,
            int platformLootCountMin,
            int platformLootCountMax,
            IReadOnlyList<LootEntryData> platformTable,
            float enemyDropChance,
            IReadOnlyList<LootEntryData> enemyDropTable,
            int questRewardCountMin,
            int questRewardCountMax,
            IReadOnlyList<LootEntryData> questTable)
        {
            Theme = theme;
            PlatformLootChance = platformLootChance;
            PlatformLootCountMin = platformLootCountMin;
            PlatformLootCountMax = platformLootCountMax;
            PlatformTable = platformTable ?? Array.Empty<LootEntryData>();
            EnemyDropChance = enemyDropChance;
            EnemyDropTable = enemyDropTable ?? Array.Empty<LootEntryData>();
            QuestRewardCountMin = questRewardCountMin;
            QuestRewardCountMax = questRewardCountMax;
            QuestTable = questTable ?? Array.Empty<LootEntryData>();
        }
    }
}
