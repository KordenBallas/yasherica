using UnityEngine;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for healing abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "HealAbility", menuName = "Combat/Abilities/Heal Ability")]
    public class HealAbilityDefinition : AbilityDefinition
    {
        [Header("Heal Settings")]
        [Tooltip("Amount of health restored")]
        [SerializeField] private int _healAmount = 15;

        public int HealAmount => _healAmount;
    }
}
