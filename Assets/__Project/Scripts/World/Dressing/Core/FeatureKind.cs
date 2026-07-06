namespace World.Dressing.Core
{
    /// <summary>
    /// How a biome feature occupies the platform (biome-decoration brief FR5–FR7): a blocking
    /// feature is a tactical obstacle that consumes its whole cell; decorative features are
    /// visual-only and never consume cells — small ones may cluster several per cell.
    /// </summary>
    public enum FeatureKind
    {
        /// <summary>Visual-only, small (tufts, pebbles, shrubs); several may share one cell.</summary>
        SmallDecorative = 0,

        /// <summary>Visual-only, large (a rock outcrop, a bush); biased to the platform rim/rear.</summary>
        LargeDecorative = 1,

        /// <summary>A natural obstacle on the combat grid; occupies its whole cell, one per cell.</summary>
        Blocking = 2
    }
}
