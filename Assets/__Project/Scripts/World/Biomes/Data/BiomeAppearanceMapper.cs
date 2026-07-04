using LevelGeneration.Route;

namespace World.Biomes.Data
{
    /// <summary>
    /// The only bridge from the <see cref="BiomeAppearanceDefinition"/> SO to the UnityEngine-free
    /// <see cref="BiomeLandscapeSettings"/> Core record (CLAUDE.md §7). Falls back to the code
    /// defaults when no asset is authored for the run's biome, so an unauthored biome still routes
    /// and reads sanely. Unity-typed fields (kits, tints) never cross — views read them off the SO.
    /// </summary>
    public static class BiomeAppearanceMapper
    {
        public static BiomeLandscapeSettings ToLandscapeSettings(BiomeAppearanceDefinition definition)
        {
            if (definition == null)
            {
                return BiomeLandscapeSettings.CreateDefault();
            }

            return new BiomeLandscapeSettings(
                definition.CorridorHalfWidth,
                definition.BaselineWavelength,
                definition.BaselineAmplitude,
                definition.ArcSlotLength,
                definition.ArcChancePercent,
                definition.ArcDepth,
                definition.ArcHalfLengthMin,
                definition.ArcHalfLengthMax,
                definition.LandmarkOffset,
                definition.LandmarkScaleMin,
                definition.LandmarkScaleMax,
                definition.TierCount,
                definition.TierStep,
                definition.TierWavelength,
                definition.BackdropRidgeAmplitude,
                definition.BackdropRidgeWavelength);
        }
    }
}
