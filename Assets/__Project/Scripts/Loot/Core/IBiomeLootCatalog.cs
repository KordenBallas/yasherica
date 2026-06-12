using LevelGeneration;

namespace Loot.Core
{
    /// <summary>
    /// Lookup of biome loot data by level theme. Implemented in the data layer
    /// over the authored BiomeLootDefinition assets.
    /// </summary>
    public interface IBiomeLootCatalog
    {
        /// <summary>
        /// Returns the loot data for the theme, or null when no biome is authored for it.
        /// </summary>
        BiomeLootData Get(LevelTheme theme);
    }
}
