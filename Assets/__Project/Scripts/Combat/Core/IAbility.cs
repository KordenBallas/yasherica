namespace Combat.Core
{
    /// <summary>
    /// Represents a base ability that a unit can use.
    /// Contains the definition and parameters of the ability.
    /// </summary>
    public interface IAbility
    {
        /// <summary>
        /// Unique identifier for this ability.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// Display name of the ability.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Number of turns before the ability can be used again after execution.
        /// 0 means no cooldown (can be used every turn).
        /// </summary>
        int CooldownDuration { get; }
        
        /// <summary>
        /// The type of target this ability can affect.
        /// </summary>
        AbilityTargetType TargetType { get; }
        
        /// <summary>
        /// Maximum range of the ability in hex cells.
        /// </summary>
        int Range { get; }
        
        /// <summary>
        /// The primary effect type of this ability.
        /// </summary>
        AbilityEffectType EffectType { get; }
    }
}

