using UnityEngine;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject for the global enemy-difficulty layer: modulates every enemy's
    /// AIProfileDefinition at once — additive decision-quality degradation, multiplicative
    /// priority scaling. Contains ONLY configuration data - NO logic.
    /// Deliberately carries NO HP/damage multipliers: stat scaling belongs to the future
    /// Heat system (Track Y), this asset only shapes how well enemies decide.
    /// </summary>
    [CreateAssetMenu(fileName = "Difficulty", menuName = "Combat/AI/Difficulty")]
    public class DifficultyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id for persistence and future difficulty selection UI")]
        [SerializeField] private string _difficultyId = "normal";

        [Tooltip("Display name for UI")]
        [SerializeField] private string _displayName = "Normal";

        [Header("Decision Quality (added to every profile's dials)")]
        [Tooltip("Extra ± score noise added to every enemy profile")]
        [Min(0f)]
        [SerializeField] private float _extraScoreNoise = 0f;

        [Tooltip("Extra candidates added to every profile's pick-from-top-N")]
        [Range(0, 9)]
        [SerializeField] private int _extraTopN = 0;

        [Tooltip("Extra mistake chance added to every profile (clamped to 1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _extraMistakeChance = 0f;

        [Header("Priority Scaling (multiplies every profile's weights)")]
        [Tooltip("Multiplies each profile's aggression weight")]
        [Range(0f, 3f)]
        [SerializeField] private float _aggressionScale = 1f;

        [Tooltip("Multiplies each profile's kill bonus (kill-securing drive)")]
        [Range(0f, 3f)]
        [SerializeField] private float _killSecuringScale = 1f;

        [Tooltip("Multiplies each profile's status-effect bonus")]
        [Range(0f, 3f)]
        [SerializeField] private float _statusValueScale = 1f;

        // Public read-only accessors
        public string DifficultyId => _difficultyId;
        public string DisplayName => _displayName;
        public float ExtraScoreNoise => _extraScoreNoise;
        public int ExtraTopN => _extraTopN;
        public float ExtraMistakeChance => _extraMistakeChance;
        public float AggressionScale => _aggressionScale;
        public float KillSecuringScale => _killSecuringScale;
        public float StatusValueScale => _statusValueScale;
    }
}
