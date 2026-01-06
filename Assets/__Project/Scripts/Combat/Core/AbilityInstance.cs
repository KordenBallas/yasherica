namespace Combat.Core
{
    /// <summary>
    /// Concrete implementation of an ability instance with cooldown tracking.
    /// Immutable - returns new instances when cooldown changes.
    /// </summary>
    public class AbilityInstance : IAbilityInstance
    {
        public IAbility Ability { get; }
        public int CurrentCooldown { get; }
        public bool IsAvailable => CurrentCooldown == 0;
        
        public AbilityInstance(IAbility ability, int currentCooldown = 0)
        {
            Ability = ability;
            CurrentCooldown = currentCooldown;
        }
        
        /// <summary>
        /// Creates a new instance with the cooldown decremented by 1.
        /// </summary>
        public AbilityInstance DecrementCooldown()
        {
            return new AbilityInstance(Ability, System.Math.Max(0, CurrentCooldown - 1));
        }
        
        /// <summary>
        /// Creates a new instance with the cooldown reset to the ability's cooldown duration.
        /// </summary>
        public AbilityInstance ResetCooldown()
        {
            return new AbilityInstance(Ability, Ability.CooldownDuration);
        }
    }
}

