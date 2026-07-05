using System.Collections.Generic;

namespace LevelGeneration.Journey
{
    /// <summary>
    /// Immutable, UnityEngine-free roster of biomes eligible for the run's biome journey. A biome
    /// not listed here (or listed with weight 0) never appears — that is the data-driven exclusion
    /// (Cave today). Mapped from the <c>BiomeProgressionConfig</c> SO at install time.
    /// </summary>
    public sealed class BiomeProgressionSettings
    {
        public IReadOnlyList<BiomeProgressionEntry> Entries { get; }

        public BiomeProgressionSettings(IReadOnlyList<BiomeProgressionEntry> entries)
        {
            Entries = entries ?? new List<BiomeProgressionEntry>();
        }

        /// <summary>
        /// Fail-safe default: a single Forest tier-1 entry — exactly the pre-journey fixed-Forest
        /// behavior — so a missing/empty config degrades gracefully instead of breaking the run.
        /// </summary>
        public static BiomeProgressionSettings CreateDefault()
        {
            return new BiomeProgressionSettings(new List<BiomeProgressionEntry>
            {
                new BiomeProgressionEntry(
                    LevelTheme.Forest, escalationTier: 1, selectionWeight: 1,
                    stretchMinWindows: 3, stretchMaxWindows: 4)
            });
        }
    }
}
