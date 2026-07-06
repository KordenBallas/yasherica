using LevelGeneration;

namespace World.Dressing.Core
{
    /// <summary>
    /// Pure lookup from a biome to its bound feature kit's pool + the biome's placement dials.
    /// A biome with no bound kit returns false — the base-layer fail-safe (dressing-kit brief FR8).
    /// </summary>
    public interface IBiomeFeaturePoolCatalog
    {
        bool TryGet(LevelTheme theme, out FeaturePoolData pool, out FeatureDensitySettings density);
    }
}
