using Combat.Core;
using UnityEngine;

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
        [SerializeField] private int _id;
        [SerializeField] private string _name;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Shape")]
        [Tooltip("Line: straight row of cells in a chosen direction. Ring: hollow circle around the caster.")]
        [SerializeField] private AbilityShapeType _shape = AbilityShapeType.Line;

        [Tooltip("Number of cells in the line (Line only)")]
        [SerializeField, Min(1)] private int _lineLength = 1;

        [Tooltip("Distance of the hollow ring from the caster (Ring only)")]
        [SerializeField, Min(1)] private int _ringRadius = 1;

        [Header("Cooldown")]
        [SerializeField] private int _cooldownDuration;

        [Header("Effect Type")]
        [SerializeField] private AbilityEffectType _effectType = AbilityEffectType.Damage;

        [Header("Displacement")]
        [Tooltip("Cells a struck unit is pushed away from the caster along the line direction; 0 = no push (Line abilities only)")]
        [SerializeField, Min(0)] private int _pushDistance;

        [Header("Visual")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _animationTrigger;

        public int Id => _id;
        public string Name => _name;
        public string Description => _description;
        public AbilityShapeType Shape => _shape;
        public int LineLength => _lineLength;
        public int RingRadius => _ringRadius;
        public int CooldownDuration => _cooldownDuration;
        public AbilityEffectType EffectType => _effectType;
        public int PushDistance => _pushDistance;
        public Sprite Icon => _icon;
        public string AnimationTrigger => _animationTrigger;
    }
}
