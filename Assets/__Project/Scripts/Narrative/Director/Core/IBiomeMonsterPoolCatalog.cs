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

        /// <summary>
        /// The theme's enemy ids whose run-tier band contains <paramref name="tier"/> (D19 escalation):
        /// as the run climbs, the pool shifts toward tougher, in-band creatures. Unbanded creatures pass
        /// at every tier. Empty when no in-band creature is authored for the theme.
        /// </summary>
        IReadOnlyList<int> GetPool(LevelTheme theme, int tier);

        /// <summary>
        /// The theme's enemy ids that both carry the flavor and are in-band for <paramref name="tier"/>.
        /// An empty/null flavor falls back to <see cref="GetPool(LevelTheme, int)"/>; an unmatched flavor
        /// returns empty — the fallback policy (unfiltered + warn) is the caller's.
        /// </summary>
        IReadOnlyList<int> GetPool(LevelTheme theme, string flavor, int tier);
    }
}
