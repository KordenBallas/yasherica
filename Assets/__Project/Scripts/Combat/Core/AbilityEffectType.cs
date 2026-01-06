namespace Combat.Core
{
    /// <summary>
    /// Defines the effect type of an ability.
    /// </summary>
    public enum AbilityEffectType
    {
        /// <summary>
        /// Ability deals damage.
        /// </summary>
        Damage,
        
        /// <summary>
        /// Ability heals target.
        /// </summary>
        Heal,
        
        /// <summary>
        /// Ability applies a status effect.
        /// </summary>
        StatusEffect,
        
        /// <summary>
        /// Ability has multiple effects (e.g., damage + status effect).
        /// </summary>
        Hybrid
    }
}

