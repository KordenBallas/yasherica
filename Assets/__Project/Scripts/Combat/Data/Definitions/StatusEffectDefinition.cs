using UnityEngine;
using Combat.Core;
using Combat.Core.StatusEffects;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for status effects.
    /// Contains ONLY configuration data - NO logic.
    /// Use StatusEffectFactory to create runtime IStatusEffect instances.
    /// Assets live under Resources/Combat/StatusEffects/ (loaded by StatusEffectDefinitionCatalog).
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

        [Tooltip("The one glyph for this status — shown on the unit on the board AND in the " +
                 "ability/card effect slot (author it once, shape-distinct, not colour-only)")]
        [SerializeField] private Sprite _glyph;

        [Header("Type")]
        [Tooltip("Category of the status effect")]
        [SerializeField] private StatusEffectType _type = StatusEffectType.Debuff;

        [Header("Duration & Stacking")]
        [Tooltip("Default duration in turns (-1 for infinite, e.g. a part passive)")]
        [SerializeField] private int _duration = 2;

        [Tooltip("How re-applying this status is resolved (Refresh is the default)")]
        [SerializeField] private StackRule _stackRule = StackRule.Refresh;

        [Tooltip("Maximum stack count for StackToCap (0 = unlimited)")]
        [SerializeField] private int _maxStacks;

        [Header("Trigger Settings")]
        [Tooltip("When this effect triggers its behavior (TurnEnd is the canonical deterministic tick)")]
        [SerializeField] private StatusEffectTriggerType _triggerType = StatusEffectTriggerType.TurnEnd;

        [Header("Effect Values (DoT / HoT)")]
        [Tooltip("Damage dealt per trigger (for DoT effects)")]
        [SerializeField] private int _damagePerTrigger;

        [Tooltip("Healing applied per trigger (for HoT effects)")]
        [SerializeField] private int _healPerTrigger;

        [Header("Control (Control type only)")]
        [Tooltip("What the control restricts: Stun = loses turn, Root = no move, Slow = reduced move")]
        [SerializeField] private ControlKind _controlKind = ControlKind.Stun;

        [Tooltip("Movement cells removed per stack (Slow only)")]
        [Min(0)]
        [SerializeField] private int _movementPenalty = 1;

        [Header("Stat Modifier (Buff/Debuff type only)")]
        [Tooltip("Which stat the modifier moves")]
        [SerializeField] private StatTarget _statTarget = StatTarget.OutgoingDamage;

        [Tooltip("Flat signed delta (e.g. Weakened = -5 outgoing, Hardened = -5 incoming)")]
        [SerializeField] private int _magnitude;

        [Header("Threshold Settings (for OnThreshold trigger)")]
        [Tooltip("HP percentage threshold (0.0 - 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float _hpThreshold = 0.5f;

        [Tooltip("Trigger when HP goes below or above threshold")]
        [SerializeField] private ThresholdDirection _thresholdDirection = ThresholdDirection.Below;

        [Header("Stack Scaling (StackToCap only)")]
        [Tooltip("Additional damage per stack")]
        [SerializeField] private int _damagePerStack;

        [Tooltip("Additional healing per stack")]
        [SerializeField] private int _healPerStack;

        // Public read-only accessors
        public int Id => _id;
        public string Name => _name;
        public string Description => _description;
        public Sprite Glyph => _glyph;
        public StatusEffectType Type => _type;
        public int Duration => _duration;
        public StackRule StackRule => _stackRule;
        public int MaxStacks => _maxStacks;
        public StatusEffectTriggerType TriggerType => _triggerType;
        public int DamagePerTrigger => _damagePerTrigger;
        public int HealPerTrigger => _healPerTrigger;
        public ControlKind ControlKind => _controlKind;
        public int MovementPenalty => _movementPenalty;
        public StatTarget StatTarget => _statTarget;
        public int Magnitude => _magnitude;
        public float HpThreshold => _hpThreshold;
        public ThresholdDirection ThresholdDirection => _thresholdDirection;
        public int DamagePerStack => _damagePerStack;
        public int HealPerStack => _healPerStack;
    }
}
