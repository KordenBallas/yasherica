namespace Combat.Core
{
    /// <summary>
    /// Defines the type of status effect.
    /// </summary>
    public enum StatusEffectType
    {
        /// <summary>
        /// Positive effect that enhances the target.
        /// </summary>
        Buff,
        
        /// <summary>
        /// Negative effect that weakens the target.
        /// </summary>
        Debuff,
        
        /// <summary>
        /// Damage over time effect (e.g., poison, bleeding).
        /// </summary>
        DamageOverTime,
        
        /// <summary>
        /// Healing over time effect (e.g., regeneration).
        /// </summary>
        HealOverTime,
        
        /// <summary>
        /// Control effect that restricts actions (e.g., stun, immobilize).
        /// </summary>
        Control
    }
}

