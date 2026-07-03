using System.Collections.Generic;
using Combat.Data.Definitions;
using Core.Logging;
using LevelGeneration;

namespace Combat.Data
{
    /// <summary>
    /// The only bridge from the authored <see cref="BiomeMonsterPoolDefinition"/> assets to the
    /// UnityEngine-free tagged entry map consumed by <c>BiomeMonsterPoolCatalog</c> (CLAUDE.md §7).
    /// Each entry carries the enemy's <c>EnemyTags</c> so site combat beats can flavor-filter the same
    /// pool. Skips null entries and warns on duplicate themes (first authored pool wins, mirroring the
    /// biome loot catalog).
    /// </summary>
    public static class BiomeMonsterPoolMapper
    {
        public static Dictionary<LevelTheme, IReadOnlyList<Narrative.Director.Core.MonsterPoolEntry>> ToPools(
            IEnumerable<BiomeMonsterPoolDefinition> definitions, IGameLogger logger = null)
        {
            var pools = new Dictionary<LevelTheme, IReadOnlyList<Narrative.Director.Core.MonsterPoolEntry>>();
            if (definitions == null)
            {
                return pools;
            }

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (pools.ContainsKey(definition.Theme))
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[BiomeMonsterPoolMapper] Duplicate monster pool for theme " +
                        $"{definition.Theme} ('{definition.name}') ignored.");
                    continue;
                }

                var entries = new List<Narrative.Director.Core.MonsterPoolEntry>();
                foreach (var enemy in definition.Enemies)
                {
                    if (enemy != null)
                    {
                        entries.Add(new Narrative.Director.Core.MonsterPoolEntry(
                            enemy.EnemyId, new List<string>(enemy.EnemyTags)));
                    }
                }

                pools.Add(definition.Theme, entries);
            }

            return pools;
        }
    }
}
