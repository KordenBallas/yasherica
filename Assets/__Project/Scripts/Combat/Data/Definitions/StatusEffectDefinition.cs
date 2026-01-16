using UnityEngine;
using Combat.Core;
using Combat.Core.StatusEffects;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for status effects.
    /// Contains ONLY configuration data - NO logic.
    /// Use StatusEffectFactory to create runtime IStatusEffect instances.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffectDefinition", menuName = "Combat/Status Effects/Status Effect")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this status effect")]
        [SerializeField] private int _id;

        [Tooltip("Display name of the effect")]
        [SerializeField] private string _name;

        [TextArea(2, 4)]
        [Tooltip("Description of the effect")]
        [SerializeField] private string _description;

        [Header("Type")]
        [Tooltip("Category of the status effect")]
        [SerializeField] private StatusEffectType _type = StatusEffectType.Debuff;

        [Header("Duration & Stacking")]
        [Tooltip("Default duration in turns (-1 for infinite)")]
        [SerializeField] private int _duration = 3;

        [Tooltip("Can this effect stack multiple times?")]
        [SerializeField] private bool _isStackable;

        [Tooltip("Maximum stack count (0 = unlimited)")]
        [SerializeField] private int _maxStacks;

        [Header("Trigger Settings")]
        [Tooltip("When this effect triggers its behavior")]
        [SerializeField] private StatusEffectTriggerType _triggerType = StatusEffectTriggerType.TurnStart;

        [Header("Effect Values")]
        [Tooltip("Damage dealt per trigger (for DoT effects)")]
        [SerializeField] private int _damagePerTrigger;

        [Tooltip("Healing applied per trigger (for HoT effects)")]
        [SerializeField] private int _healPerTrigger;

        [Tooltip("Stat modifier percentage (for Buff/Debuff effects)")]
        [Range(-1f, 1f)]
        [SerializeField] private float _statModifier;

        [Header("Threshold Settings (for OnThreshold trigger)")]
        [Tooltip("HP percentage threshold (0.0 - 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float _hpThreshold = 0.5f;

        [Tooltip("Trigger when HP goes below or above threshold")]
        [SerializeField] private ThresholdDirection _thresholdDirection = ThresholdDirection.Below;

        [Header("Stack Scaling")]
        [Tooltip("Additional damage per stack")]
        [SerializeField] private int _damagePerStack = 2;

        [Tooltip("Additional healing per stack")]
        [SerializeField] private int _healPerStack = 1;

        // Public read-only accessors
        public int Id => _id;
        public string Name => _name;
        public string Description => _description;
        public StatusEffectType Type => _type;
        public int Duration => _duration;
        public bool IsStackable => _isStackable;
        public int MaxStacks => _maxStacks;
        public StatusEffectTriggerType TriggerType => _triggerType;
        public int DamagePerTrigger => _damagePerTrigger;
        public int HealPerTrigger => _healPerTrigger;
        public float StatModifier => _statModifier;
        public float HpThreshold => _hpThreshold;
        public ThresholdDirection ThresholdDirection => _thresholdDirection;
        public int DamagePerStack => _damagePerStack;
        public int HealPerStack => _healPerStack;
    }
}
