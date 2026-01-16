using UnityEngine;
using Combat.Data;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for AI behavior profiles.
    /// Extends base AI personalities with configurable parameters.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "AIProfile", menuName = "Combat/AI/AI Profile")]
    public class AIProfileDefinition : ScriptableObject
    {
        [Header("Base Personality")]
        [Tooltip("Base AI decision-making strategy")]
        [SerializeField] private AIPersonality _basePersonality = AIPersonality.SimpleRandom;

        [Header("Movement Settings")]
        [Tooltip("Maximum hex cells the AI will consider moving")]
        [SerializeField] private int _movementRange = 3;

        [Header("Tactical Weights (for Tactical AI)")]
        [Tooltip("Weight multiplier for damage scoring")]
        [SerializeField] private float _damageWeight = 2.0f;

        [Tooltip("Weight multiplier for healing scoring")]
        [SerializeField] private float _healWeight = 1.5f;

        [Tooltip("Bonus score for potential kills (low HP enemies)")]
        [SerializeField] private float _killBonus = 50f;

        [Tooltip("HP threshold for kill bonus consideration (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _killThresholdPercent = 0.3f;

        [Tooltip("Bonus for status effect abilities")]
        [SerializeField] private float _statusEffectBonus = 30f;

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
        public float KillThresholdPercent => _killThresholdPercent;
        public float StatusEffectBonus => _statusEffectBonus;
        public float DefensiveHpThreshold => _defensiveHpThreshold;
        public float CloseRangeBonus => _closeRangeBonus;
        public float SurroundPenalty => _surroundPenalty;
    }
}
