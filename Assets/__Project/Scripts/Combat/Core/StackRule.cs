namespace Combat.Core
{
    /// <summary>
    /// How re-applying a status that is already on a unit is resolved. Per-status data
    /// (authored on StatusEffectDefinition); Refresh is the default so nothing piles up
    /// unless a status explicitly opts in.
    /// </summary>
    public enum StackRule
    {
        /// <summary>Re-application resets the duration to the fresh value (no pile-up).</summary>
        Refresh = 0,

        /// <summary>
        /// Re-application adds a stack up to MaxStacks (magnitude scales per the status's
        /// stack-scaling values) and refreshes the duration — even when already at the cap.
        /// </summary>
        StackToCap = 1,

        /// <summary>Re-application is ignored while the status is active (no chain-lock).</summary>
        Ignore = 2
    }
}
