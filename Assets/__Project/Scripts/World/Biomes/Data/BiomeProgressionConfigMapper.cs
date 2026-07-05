using System.Collections.Generic;
using Core.Logging;
using LevelGeneration.Journey;

namespace World.Biomes.Data
{
    /// <summary>
    /// The only bridge from the <see cref="BiomeProgressionConfig"/> SO to the UnityEngine-free
    /// <see cref="BiomeProgressionSettings"/> Core record (CLAUDE.md §7). Falls back to the fixed
    /// Forest default when no config asset is wired; duplicate themes keep the first authored entry
    /// (the catalog convention).
    /// </summary>
    public static class BiomeProgressionConfigMapper
    {
        public static BiomeProgressionSettings ToSettings(BiomeProgressionConfig config, IGameLogger logger = null)
        {
            if (config == null)
            {
                logger?.Warning(LogCategory.LevelGeneration,
                    "[BiomeProgressionConfigMapper] No BiomeProgressionConfig wired; using the fixed Forest default.");
                return BiomeProgressionSettings.CreateDefault();
            }

            var entries = new List<BiomeProgressionEntry>();
            var seenThemes = new HashSet<LevelGeneration.LevelTheme>();
            foreach (var entry in config.Biomes)
            {
                if (entry == null)
                {
                    continue;
                }

                if (!seenThemes.Add(entry.Theme))
                {
                    logger?.Warning(LogCategory.LevelGeneration,
                        $"[BiomeProgressionConfigMapper] Duplicate entry for {entry.Theme}; first authored wins.");
                    continue;
                }

                entries.Add(new BiomeProgressionEntry(
                    entry.Theme, entry.EscalationTier, entry.SelectionWeight,
                    entry.StretchMinWindows, entry.StretchMaxWindows));
            }

            return new BiomeProgressionSettings(entries);
        }
    }
}
