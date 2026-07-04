using LevelGeneration;
using World.Biomes.Data;

namespace World.Biomes
{
    /// <summary>
    /// Lookup of the authored biome appearance asset by level theme. Unity-side by design: the
    /// definition carries view-facing kits/tints; the pure landscape dials cross into Core only
    /// through <see cref="BiomeAppearanceMapper"/>.
    /// </summary>
    public interface IBiomeAppearanceCatalog
    {
        /// <summary>The appearance asset for the theme, or null when none is authored.</summary>
        BiomeAppearanceDefinition Get(LevelTheme theme);
    }
}
