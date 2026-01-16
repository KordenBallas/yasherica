using UnityEngine;
using Combat.Core;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// Base ScriptableObject for ability data definitions.
    /// Contains ONLY configuration data - NO logic.
    /// Use AbilityFactory to create runtime IAbility instances.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityDefinition", menuName = "Combat/Abilities/Base Ability")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this ability")]
        [SerializeField] private int _id;

        [Tooltip("Display name of the ability")]
        [SerializeField] private string _name;

        [TextArea(2, 4)]
        [Tooltip("Description of the ability")]
        [SerializeField] private string _description;

        [Header("Targeting")]
        [Tooltip("Type of target this ability affects")]
        [SerializeField] private AbilityTargetType _targetType = AbilityTargetType.Enemy;

        [Tooltip("Maximum range in hex cells")]
        [SerializeField] private int _range = 1;

        [Header("Cooldown")]
        [Tooltip("Turns before ability can be used again (0 = no cooldown)")]
        [SerializeField] private int _cooldownDuration;

        [Header("Effect Type")]
        [Tooltip("Primary effect type of this ability")]
        [SerializeField] private AbilityEffectType _effectType = AbilityEffectType.Damage;

        // Public read-only accessors
        public int Id => _id;
        public string Name => _name;
        public string Description => _description;
        public AbilityTargetType TargetType => _targetType;
        public int Range => _range;
        public int CooldownDuration => _cooldownDuration;
        public AbilityEffectType EffectType => _effectType;
    }
}
