using UnityEngine;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for abilities that apply status effects.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffectAbility", menuName = "Combat/Abilities/Status Effect Ability")]
    public class StatusEffectAbilityDefinition : AbilityDefinition
    {
        [Header("Status Effect Settings")]
        [Tooltip("Status effect to apply")]
        [SerializeField] private StatusEffectDefinition _statusEffect;

        [Tooltip("Duration override (-1 uses effect's default duration)")]
        [SerializeField] private int _durationOverride = -1;

        public StatusEffectDefinition StatusEffect => _statusEffect;
        public int DurationOverride => _durationOverride;
    }
}
