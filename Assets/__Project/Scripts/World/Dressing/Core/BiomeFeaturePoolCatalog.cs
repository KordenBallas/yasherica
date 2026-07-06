using System.Collections.Generic;
using LevelGeneration;

namespace World.Dressing.Core
{
    /// <summary>
    /// Dictionary-backed <see cref="IBiomeFeaturePoolCatalog"/> built once at install time by the
    /// kit mapper. A biome absent from the map has no bound kit — base layer (brief FR8).
    /// </summary>
    public sealed class BiomeFeaturePoolCatalog : IBiomeFeaturePoolCatalog
    {
        private readonly Dictionary<LevelTheme, (FeaturePoolData Pool, FeatureDensitySettings Density)> _pools;

        public BiomeFeaturePoolCatalog(
            Dictionary<LevelTheme, (FeaturePoolData Pool, FeatureDensitySettings Density)> pools)
        {
            _pools = pools ?? new Dictionary<LevelTheme, (FeaturePoolData, FeatureDensitySettings)>();
        }

        public bool TryGet(LevelTheme theme, out FeaturePoolData pool, out FeatureDensitySettings density)
        {
            if (_pools.TryGetValue(theme, out var entry))
            {
                pool = entry.Pool;
                density = entry.Density;
                return true;
            }

            pool = null;
            density = null;
            return false;
        }
    }
}
