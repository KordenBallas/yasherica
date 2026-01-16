namespace Combat.Core.StatusEffects
{
    /// <summary>
    /// Defines when a status effect triggers its behavior.
    /// Uses flags to allow multiple trigger types per effect.
    /// </summary>
    [System.Flags]
    public enum StatusEffectTriggerType
    {
        None = 0,

        /// <summary>At the start of the affected unit's turn.</summary>
        TurnStart = 1 << 0,

        /// <summary>At the end of the affected unit's turn.</summary>
        TurnEnd = 1 << 1,

        /// <summary>When the affected unit attacks.</summary>
        OnAttack = 1 << 2,

        /// <summary>When the affected unit is hit by an attack.</summary>
        OnHit = 1 << 3,

        /// <summary>When HP crosses a threshold (e.g., below 50%).</summary>
        OnThreshold = 1 << 4,

        /// <summary>When the effect is first applied.</summary>
        OnApply = 1 << 5,

        /// <summary>When the effect expires or is removed.</summary>
        OnRemove = 1 << 6
    }
}
