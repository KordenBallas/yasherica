namespace World.Dressing.Core
{
    /// <summary>
    /// The per-biome placement dials (biome-decoration brief FR5–FR8). These live on the consuming
    /// seam (the biome appearance config), NOT on the kit: a kit supplies which assets exist;
    /// density, placement, and seeding stay owned by the seam (dressing-kit brief FR7).
    /// </summary>
    public sealed class FeatureDensitySettings
    {
        public const float DefaultBlockersPer100Cells = 4f;
        public const float DefaultDecorClustersPer100Cells = 10f;
        public const float DefaultLaneHalfWidth = 1.1f;

        public FeatureDensitySettings(
            float blockersPer100Cells = DefaultBlockersPer100Cells,
            float decorClustersPer100Cells = DefaultDecorClustersPer100Cells,
            float laneHalfWidth = DefaultLaneHalfWidth)
        {
            BlockersPer100Cells = blockersPer100Cells < 0f ? 0f : blockersPer100Cells;
            DecorClustersPer100Cells = decorClustersPer100Cells < 0f ? 0f : decorClustersPer100Cells;
            LaneHalfWidth = laneHalfWidth < 0f ? 0f : laneHalfWidth;
        }

        /// <summary>Blocking obstacles per 100 surface cells — a floor-not-target, kept sparse.</summary>
        public float BlockersPer100Cells { get; }

        /// <summary>Decorative clusters (copse / outcrop / tuft patch) per 100 surface cells.</summary>
        public float DecorClustersPer100Cells { get; }

        /// <summary>Half-width (world units) of the protected straight movement lane along local Z≈0.</summary>
        public float LaneHalfWidth { get; }

        public static FeatureDensitySettings CreateDefault() => new FeatureDensitySettings();
    }
}
