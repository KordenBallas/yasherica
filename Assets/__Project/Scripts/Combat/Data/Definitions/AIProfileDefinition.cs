using UnityEngine;
using Combat.Data;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for AI behavior profiles.
    /// Extends base AI personalities with configurable parameters.
    /// Contains ONLY configuration data - NO logic.
    /// Defaults reproduce a "sharp" enemy: perfect decision quality, neutral priorities —
    /// legacy assets that lack the newer fields keep behaving as before.
    /// </summary>
    [CreateAssetMenu(fileName = "AIProfile", menuName = "Combat/AI/AI Profile")]
    public class AIProfileDefinition : ScriptableObject
    {
        [Header("Base Personality")]
        [Tooltip("Base AI decision-making strategy")]
        [SerializeField] private AIPersonality _basePersonality = AIPersonality.SimpleRandom;

        [Header("Movement Settings")]
        [Tooltip("Maximum hex cells the AI will consider moving")]
        [Range(0, 5)]
        [SerializeField] private int _movementRange = 3;

        [Header("Tactical Weights (for Tactical AI)")]
        [Tooltip("Weight multiplier for damage scoring")]
        [SerializeField] private float _damageWeight = 2.0f;

        [Tooltip("Weight multiplier for healing scoring")]
        [SerializeField] private float _healWeight = 1.5f;

        [Tooltip("Bonus score when a hit is predicted to finish a hostile")]
        [SerializeField] private float _killBonus = 50f;

        [Tooltip("Bonus for status effect abilities")]
        [SerializeField] private float _statusEffectBonus = 30f;

        [Tooltip("Bonus per hostile hit, scaled by its missing-HP fraction (focus the wounded)")]
        [Min(0f)]
        [SerializeField] private float _focusWoundedWeight = 25f;

        [Tooltip("Multiplier on damage dealt to non-hostiles (subtracted from the score)")]
        [Min(0f)]
        [SerializeField] private float _friendlyFirePenaltyWeight = 2f;

        [Tooltip("Scales all damage and kill terms (offense priority)")]
        [Range(0f, 3f)]
        [SerializeField] private float _aggressionWeight = 1f;

        [Tooltip("Scales the retreat/defensive positioning term when wounded")]
        [Range(0f, 3f)]
        [SerializeField] private float _selfPreservationWeight = 1f;

        [Header("Decision Quality (difficulty dials)")]
        [Tooltip("± uniform noise added to every candidate score before picking (0 = exact)")]
        [Range(0f, 50f)]
        [SerializeField] private float _scoreNoise = 0f;

        [Tooltip("Pick uniformly among the N best candidates (1 = always the best)")]
        [Range(1, 10)]
        [SerializeField] private int _pickFromTopN = 1;

        [Tooltip("Chance to pick a uniformly random action outright (0 = never)")]
        [Range(0f, 1f)]
        [SerializeField] private float _mistakeChance = 0f;

        [Header("Positioning Preferences")]
        [Tooltip("HP threshold for defensive positioning (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _defensiveHpThreshold = 0.5f;

        [Tooltip("Score bonus for close range (aggressive)")]
        [SerializeField] private float _closeRangeBonus = 20f;

        [Tooltip("Score penalty for being surrounded by enemies")]
        [SerializeField] private float _surroundPenalty = 15f;

        // Public read-only accessors
        public AIPersonality BasePersonality => _basePersonality;
        public int MovementRange => _movementRange;
        public float DamageWeight => _damageWeight;
        public float HealWeight => _healWeight;
        public float KillBonus => _killBonus;
        public float StatusEffectBonus => _statusEffectBonus;
        public float FocusWoundedWeight => _focusWoundedWeight;
        public float FriendlyFirePenaltyWeight => _friendlyFirePenaltyWeight;
        public float AggressionWeight => _aggressionWeight;
        public float SelfPreservationWeight => _selfPreservationWeight;
        public float ScoreNoise => _scoreNoise;
        public int PickFromTopN => _pickFromTopN;
        public float MistakeChance => _mistakeChance;
        public float DefensiveHpThreshold => _defensiveHpThreshold;
        public float CloseRangeBonus => _closeRangeBonus;
        public float SurroundPenalty => _surroundPenalty;
    }
}
