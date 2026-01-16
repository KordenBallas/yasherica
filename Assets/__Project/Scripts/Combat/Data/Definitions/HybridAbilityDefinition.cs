using UnityEngine;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for hybrid abilities (damage + status effect).
    /// </summary>
    [CreateAssetMenu(fileName = "HybridAbility", menuName = "Combat/Abilities/Hybrid Ability")]
    public class HybridAbilityDefinition : AbilityDefinition
    {
        [Header("Damage Settings")]
        [Tooltip("Base damage amount")]
        [SerializeField] private int _damage = 8;

        [Header("Status Effect Settings")]
        [Tooltip("Status effect to apply")]
        [SerializeField] private StatusEffectDefinition _statusEffect;

        [Tooltip("Duration override (-1 uses effect's default duration)")]
        [SerializeField] private int _durationOverride = -1;

        public int Damage => _damage;
        public StatusEffectDefinition StatusEffect => _statusEffect;
        public int DurationOverride => _durationOverride;
    }
}
