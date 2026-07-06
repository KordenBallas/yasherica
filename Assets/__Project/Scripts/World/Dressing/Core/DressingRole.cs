namespace World.Dressing.Core
{
    /// <summary>
    /// Which kit list a placement's entry index resolves against — the role vocabulary of the
    /// dressing-kit contract (dressing-kit brief FR1): biome kits expose one feature list; site
    /// kits expose the structure/prop/focal/gate roles of the site-dressing design.
    /// </summary>
    public enum DressingRole
    {
        /// <summary>A biome-feature-kit entry (rock / tree / cactus / tuft).</summary>
        Feature = 0,

        /// <summary>A site structure (house) — the skyline; blocking.</summary>
        Structure = 1,

        /// <summary>A small site prop (sack / barrel / crate) — decorative.</summary>
        Prop = 2,

        /// <summary>The site's focal piece (the camp fire) — blocking, gathered around.</summary>
        Focal = 3,

        /// <summary>A threshold/gate piece on the block's approach edge — decorative.</summary>
        Gate = 4
    }
}
