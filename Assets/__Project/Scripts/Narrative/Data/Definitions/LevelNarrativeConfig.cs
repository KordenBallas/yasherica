using LevelGeneration;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// Configuration for narrative density and filtering per level.
    /// Assigned to the installer to control how many stories/NPCs appear.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelNarrativeConfig", menuName = "Narrative/Level Narrative Config")]
    public class LevelNarrativeConfig : ScriptableObject
    {
        [Header("Story Density")]
        [SerializeField] private int _minStories = 1;
        [SerializeField] private int _maxStories = 3;

        [Header("NPC Density")]
        [SerializeField] private int _minNpcs = 1;
        [SerializeField] private int _maxNpcs = 4;

        [Header("Filters")]
        [SerializeField] private LevelTheme _theme;
        [SerializeField] private int _difficulty;
        [SerializeField] private string[] _requiredTags;
        [SerializeField] private string[] _excludedTags;

        public int MinStories => _minStories;
        public int MaxStories => _maxStories;
        public int MinNpcs => _minNpcs;
        public int MaxNpcs => _maxNpcs;
        public LevelTheme Theme => _theme;
        public int Difficulty => _difficulty;
        public System.Collections.Generic.IReadOnlyList<string> RequiredTags => _requiredTags;
        public System.Collections.Generic.IReadOnlyList<string> ExcludedTags => _excludedTags;

        private void OnValidate()
        {
            if (_minStories < 0) _minStories = 0;
            if (_maxStories < _minStories) _maxStories = _minStories;
            if (_minNpcs < 0) _minNpcs = 0;
            if (_maxNpcs < _minNpcs) _maxNpcs = _minNpcs;
            if (_difficulty < 0) _difficulty = 0;
        }
    }
}
