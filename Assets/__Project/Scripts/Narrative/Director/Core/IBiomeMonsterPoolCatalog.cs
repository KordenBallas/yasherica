using System.Collections.Generic;
using LevelGeneration;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Lookup of the ambient-monster pool (enemy ids) per biome theme. Built in the data layer from the
    /// authored <c>BiomeMonsterPoolDefinition</c> assets; consumed by the allocator when a platform slot
    /// is allocated as ambient combat (Combat·wild-beast, flat difficulty).
    /// </summary>
    public interface IBiomeMonsterPoolCatalog
    {
        /// <summary>The enemy ids authored for the theme; empty when no pool is authored for it.</summary>
        IReadOnlyList<int> GetPool(LevelTheme theme);
    }
}
