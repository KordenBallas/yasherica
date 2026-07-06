namespace World.Dressing.Core
{
    /// <summary>Which kit kind a platform's dressing plan resolved against (or none — base layer).</summary>
    public enum DressingPlanKind
    {
        /// <summary>No kit bound / unresolvable — the fail-safe base layer (dressing-kit brief FR8).</summary>
        None = 0,

        BiomeFeatures = 1,

        SiteDressing = 2
    }
}
