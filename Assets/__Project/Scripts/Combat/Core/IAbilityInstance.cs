namespace Combat.Core
{
    /// <summary>
    /// Represents a specific instance of an ability on a unit.
    /// Tracks the cooldown state of the ability.
    /// </summary>
    public interface IAbilityInstance
    {
        /// <summary>
        /// The ability definition.
        /// </summary>
        IAbility Ability { get; }
        
        /// <summary>
        /// Remaining turns until the ability becomes available.
        /// 0 means the ability is ready to use.
        /// </summary>
        int CurrentCooldown { get; }
        
        /// <summary>
        /// Indicates whether the ability can currently be used (CurrentCooldown == 0).
        /// </summary>
        bool IsAvailable { get; }
    }
}

