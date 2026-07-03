using System.Collections.Generic;
using LevelGeneration;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Lookup of the ambient-monster pool (enemy ids) per biome theme. Built in the data layer from the
    /// authored <c>BiomeMonsterPoolDefinition</c> assets; consumed by the allocator when a platform slot
    /// is allocated as ambient combat (Combat·wild-beast, flat difficulty) or as a site combat beat.
    /// </summary>
    public interface IBiomeMonsterPoolCatalog
    {
        /// <summary>The enemy ids authored for the theme; empty when no pool is authored for it.</summary>
        IReadOnlyList<int> GetPool(LevelTheme theme);

        /// <summary>
        /// The theme's enemy ids whose tags carry the flavor (e.g. "bandit"). An empty/null flavor
        /// returns the unfiltered pool; an unmatched flavor returns empty — the fallback policy
        /// (unfiltered + warn) is the caller's.
        /// </summary>
        IReadOnlyList<int> GetPool(LevelTheme theme, string flavor);
    }
}
