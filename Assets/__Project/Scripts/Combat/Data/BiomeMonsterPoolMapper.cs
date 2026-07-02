using System.Collections.Generic;
using Combat.Data.Definitions;
using Core.Logging;
using LevelGeneration;

namespace Combat.Data
{
    /// <summary>
    /// The only bridge from the authored <see cref="BiomeMonsterPoolDefinition"/> assets to the
    /// UnityEngine-free enemy-id map consumed by <c>BiomeMonsterPoolCatalog</c> (CLAUDE.md §7). Skips
    /// null entries and warns on duplicate themes (first authored pool wins, mirroring the biome loot
    /// catalog).
    /// </summary>
    public static class BiomeMonsterPoolMapper
    {
        public static Dictionary<LevelTheme, IReadOnlyList<int>> ToPools(
            IEnumerable<BiomeMonsterPoolDefinition> definitions, IGameLogger logger = null)
        {
            var pools = new Dictionary<LevelTheme, IReadOnlyList<int>>();
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

                var ids = new List<int>();
                foreach (var enemy in definition.Enemies)
                {
                    if (enemy != null)
                    {
                        ids.Add(enemy.EnemyId);
                    }
                }

                pools.Add(definition.Theme, ids);
            }

            return pools;
        }
    }
}
