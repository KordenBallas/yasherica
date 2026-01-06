namespace Combat.Core
{
    /// <summary>
    /// Interface for abilities that apply status effects.
    /// </summary>
    public interface IStatusEffectAbility : IAbility
    {
        /// <summary>
        /// The status effect to apply.
        /// </summary>
        IStatusEffect EffectToApply { get; }
        
        /// <summary>
        /// Duration of the effect in turns.
        /// </summary>
        int EffectDuration { get; }
    }
}

