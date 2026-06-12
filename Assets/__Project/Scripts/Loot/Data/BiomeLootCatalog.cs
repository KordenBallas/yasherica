using System.Collections.Generic;
using Core.Logging;
using LevelGeneration;
using Loot.Core;
using Loot.Data.Definitions;

namespace Loot.Data
{
    /// <summary>
    /// Converts authored BiomeLootDefinition assets into plain BiomeLootData
    /// snapshots once at construction so the domain stays UnityEngine-free.
    /// </summary>
    public class BiomeLootCatalog : IBiomeLootCatalog
    {
        private readonly Dictionary<LevelTheme, BiomeLootData> _biomes =
            new Dictionary<LevelTheme, BiomeLootData>();

        public BiomeLootCatalog(IEnumerable<BiomeLootDefinition> definitions, IGameLogger logger)
        {
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (_biomes.ContainsKey(definition.Theme))
                {
                    logger.Warning(
                        $"[BiomeLootCatalog] Duplicate biome loot definition for theme " +
                        $"{definition.Theme} ('{definition.name}') ignored.");
                    continue;
                }

                _biomes.Add(definition.Theme, ToData(definition));
            }
        }

        public BiomeLootData Get(LevelTheme theme)
        {
            return _biomes.TryGetValue(theme, out var biome) ? biome : null;
        }

        private static BiomeLootData ToData(BiomeLootDefinition definition)
        {
            return new BiomeLootData(
                definition.Theme,
                definition.PlatformLootChance,
                definition.PlatformLootCountRange.x,
                definition.PlatformLootCountRange.y,
                ToEntries(definition.PlatformTable),
                definition.EnemyDropChance,
                ToEntries(definition.EnemyDropTable),
                definition.QuestRewardCountRange.x,
                definition.QuestRewardCountRange.y,
                ToEntries(definition.QuestTable));
        }

        private static IReadOnlyList<LootEntryData> ToEntries(WeightedArtifactEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return System.Array.Empty<LootEntryData>();
            }

            var result = new List<LootEntryData>(entries.Length);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.ArtifactId))
                {
                    continue;
                }

                result.Add(new LootEntryData(
                    entry.ArtifactId,
                    entry.Weight,
                    entry.BiasTags,
                    entry.MinPlayerLevel,
                    entry.RequiredAchievements,
                    entry.RequiredPastQuests));
            }

            return result;
        }
    }
}
