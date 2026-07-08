namespace Heat.Core
{
    /// <summary>
    /// The rule seams a Heat modifier can pull (heat-ascension FR8). Each kind is a code-implemented
    /// seam in exactly one consuming system; authoring new modifier *instances* over these kinds is
    /// data-only (FR13), while a new kind is by definition a new seam and therefore code.
    /// </summary>
    public enum HeatEffectKind
    {
        /// <summary>Enemies resolve before the player every round, not just the ambush opening.</summary>
        EnemiesActFirst = 0,

        /// <summary>The run's escalation tier is lifted by the magnitude — a tougher creature/story pool floor (rides D19; pool shift, never a stat).</summary>
        RaisedCreatureFloor = 1,

        /// <summary>The cauldron offers fewer unseal variant options (magnitude = options cut, floored at one).</summary>
        StingyCauldron = 2,

        /// <summary>Every part blank carries fewer sockets (magnitude = sockets cut, floored at one).</summary>
        FewerSockets = 3
    }
}
