namespace Combat.Core
{
    /// <summary>
    /// Represents a temporary effect applied to a unit.
    /// Can modify characteristics or apply damage/healing over time.
    /// </summary>
    public interface IStatusEffect
    {
        /// <summary>
        /// Unique identifier for this status effect type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Display name of the status effect.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The type of status effect.
        /// </summary>
        StatusEffectType Type { get; }

        /// <summary>
        /// Remaining turns for this effect. -1 means infinite duration.
        /// </summary>
        int Duration { get; }

        /// <summary>
        /// Number of times this effect is stacked (some effects can stack).
        /// </summary>
        int StackCount { get; }

        /// <summary>
        /// How re-applying this effect while it is already active is resolved.
        /// </summary>
        StackRule StackRule { get; }
    }
}
