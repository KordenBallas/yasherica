using UnityEngine;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for damage-dealing abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageAbility", menuName = "Combat/Abilities/Damage Ability")]
    public class DamageAbilityDefinition : AbilityDefinition
    {
        [Header("Damage Settings")]
        [Tooltip("Base damage amount")]
        [SerializeField] private int _damage = 10;

        public int Damage => _damage;
    }
}
