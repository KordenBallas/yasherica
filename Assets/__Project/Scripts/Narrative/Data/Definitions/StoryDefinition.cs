using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// Flat ScriptableObject definition for stories.
    /// Replaces the BaseStoryDefinition hierarchy with a single, configurable type.
    /// Stories are generic Ink files assigned to NPCs at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDefinition", menuName = "Narrative/Story/Story")]
    public class StoryDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _storyId;
        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Ink Content")]
        [SerializeField] private TextAsset _inkJsonAsset;
        [SerializeField] private string _startingKnot = "start";

        [Header("Filters")]
        [SerializeField] private LevelTheme[] _allowedThemes;
        [SerializeField] private int _minDifficulty;
        [SerializeField] private int _maxDifficulty;
        [SerializeField] private string[] _tags;

        [Header("Behavior")]
        [SerializeField] private bool _isRepeatable;
        [SerializeField] private int _cooldownRuns;
        [SerializeField] private bool _canTransitionToCombat;

        [Header("Rewards")]
        [SerializeField] private RewardSlot[] _rewards;

        public string StoryId => _storyId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public TextAsset InkJsonAsset => _inkJsonAsset;
        public string StartingKnot => _startingKnot;
        public IReadOnlyList<LevelTheme> AllowedThemes => _allowedThemes;
        public int MinDifficulty => _minDifficulty;
        public int MaxDifficulty => _maxDifficulty;
        public IReadOnlyList<string> Tags => _tags;
        public bool IsRepeatable => _isRepeatable;
        public int CooldownRuns => _cooldownRuns;
        public bool CanTransitionToCombat => _canTransitionToCombat;
        public IReadOnlyList<RewardSlot> Rewards => _rewards;

        public bool HasInkContent => _inkJsonAsset != null;

        public string GetInkJson()
        {
            return _inkJsonAsset != null ? _inkJsonAsset.text : string.Empty;
        }

        public bool MatchesTheme(LevelTheme theme)
        {
            if (_allowedThemes == null || _allowedThemes.Length == 0)
                return true;

            for (int i = 0; i < _allowedThemes.Length; i++)
            {
                if (_allowedThemes[i] == theme)
                    return true;
            }
            return false;
        }

        public bool MatchesDifficulty(int difficulty)
        {
            if (_minDifficulty == 0 && _maxDifficulty == 0)
                return true;

            return difficulty >= _minDifficulty
                && (_maxDifficulty == 0 || difficulty <= _maxDifficulty);
        }

        public bool HasTag(string tag)
        {
            if (_tags == null || _tags.Length == 0)
                return false;

            for (int i = 0; i < _tags.Length; i++)
            {
                if (_tags[i] == tag)
                    return true;
            }
            return false;
        }

        private void OnValidate()
        {
            if (_cooldownRuns < 0)
                _cooldownRuns = 0;
            if (_minDifficulty < 0)
                _minDifficulty = 0;
            if (_maxDifficulty < 0)
                _maxDifficulty = 0;
            if (_maxDifficulty > 0 && _minDifficulty > _maxDifficulty)
                _minDifficulty = _maxDifficulty;
        }
    }

    /// <summary>
    /// Links a reward to a story with probability and condition.
    /// </summary>
    [System.Serializable]
    public class RewardSlot
    {
        [SerializeField] private RewardDefinition _reward;

        [Range(0f, 1f)]
        [SerializeField] private float _probability = 1f;

        [SerializeField] private string _condition;

        public RewardDefinition Reward => _reward;
        public float Probability => _probability;
        public string Condition => _condition;
    }
}
