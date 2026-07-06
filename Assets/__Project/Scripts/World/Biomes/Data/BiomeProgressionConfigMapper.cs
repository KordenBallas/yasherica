using System.Collections.Generic;
using Core.Logging;
using LevelGeneration.Journey;

namespace World.Biomes.Data
{
    /// <summary>
    /// The only bridge from the <see cref="BiomeProgressionConfig"/> SO to the UnityEngine-free
    /// <see cref="BiomeProgressionSettings"/> Core record (CLAUDE.md §7). Falls back to the fixed
    /// Forest default when no config asset is wired; duplicate (theme, tier) pairs keep the first
    /// authored entry (the catalog convention) — the same theme at different tiers is legitimate.
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
            // Deduped on the (theme, tier) pair — the same theme at SEVERAL tiers is legitimate
            // authoring (O1: the homelands sit at tier 1 as the entry pool AND at their climb tier),
            // compared post-normalization so a sloppy tier 0 still collides with its tier-1 twin.
            var seenKeys = new HashSet<(LevelGeneration.LevelTheme, int)>();
            foreach (var entry in config.Biomes)
            {
                if (entry == null)
                {
                    continue;
                }

                var mapped = new BiomeProgressionEntry(
                    entry.Theme, entry.EscalationTier, entry.SelectionWeight,
                    entry.StretchMinWindows, entry.StretchMaxWindows);
                if (!seenKeys.Add((mapped.Theme, mapped.EscalationTier)))
                {
                    logger?.Warning(LogCategory.LevelGeneration,
                        $"[BiomeProgressionConfigMapper] Duplicate entry for {mapped.Theme} at tier " +
                        $"{mapped.EscalationTier}; first authored wins.");
                    continue;
                }

                entries.Add(mapped);
            }

            return new BiomeProgressionSettings(entries);
        }
    }
}
