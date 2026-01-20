using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// Abstract base class for all story definitions.
    /// Provides unified structure for chapters, side stories, dialogues, and events.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    public abstract class BaseStoryDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this story")]
        [SerializeField] protected string _storyId;

        [Tooltip("Display name of the story")]
        [SerializeField] protected string _displayName;

        [TextArea(3, 6)]
        [Tooltip("Description of the story")]
        [SerializeField] protected string _description;

        [Tooltip("Type of story content")]
        [SerializeField] protected StoryType _storyType;

        [Header("Ink Content")]
        [Tooltip("Compiled Ink JSON asset")]
        [SerializeField] protected TextAsset _inkJsonAsset;

        [Tooltip("Starting knot for this story")]
        [SerializeField] protected string _startingKnot = "start";

        [Header("Story Graph")]
        [Tooltip("Relationships to other stories")]
        [SerializeField] protected List<StoryRelationship> _relationships = new();

        [Tooltip("Attributes for dynamic story connections")]
        [SerializeField] protected List<StoryAttribute> _attributes = new();

        [Header("Prerequisites")]
        [Tooltip("Prerequisites that must be met for this story to be available")]
        [SerializeField] protected StoryPrerequisites _prerequisites = new();

        [Header("Platform Configuration")]
        [Tooltip("Platform configuration for this story")]
        [SerializeField] protected StoryPlatformConfig _platformConfig = new();

        // Public read-only accessors
        public string StoryId => _storyId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public StoryType StoryType => _storyType;
        public TextAsset InkJsonAsset => _inkJsonAsset;
        public string StartingKnot => _startingKnot;
        public IReadOnlyList<StoryRelationship> Relationships => _relationships;
        public IReadOnlyList<StoryAttribute> Attributes => _attributes;
        public StoryPrerequisites Prerequisites => _prerequisites;
        public StoryPlatformConfig PlatformConfig => _platformConfig;

        /// <summary>
        /// Checks if this story has Ink content assigned.
        /// </summary>
        public bool HasInkContent => _inkJsonAsset != null;

        /// <summary>
        /// Gets the JSON content of the Ink story.
        /// </summary>
        public string GetInkJson()
        {
            return _inkJsonAsset != null ? _inkJsonAsset.text : string.Empty;
        }

        /// <summary>
        /// Gets the platform type for this story.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract StoryPlatformType GetPlatformType();

        /// <summary>
        /// Checks if this story can be repeated.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract bool CanRepeat();

        /// <summary>
        /// Gets the theme for this story (may return null for default theme).
        /// </summary>
        public virtual LevelTheme? GetTheme()
        {
            return _platformConfig.OverrideTheme ? _platformConfig.ThemeOverride : null;
        }

        /// <summary>
        /// Gets the base priority for selection (higher = more likely).
        /// </summary>
        public virtual int GetBasePriority()
        {
            return _platformConfig.BasePriority;
        }
    }

    /// <summary>
    /// Type of story content.
    /// </summary>
    public enum StoryType
    {
        Chapter,    // Main story chapter
        SideStory,  // Optional side content
        Dialogue,   // NPC dialogue
        Event       // One-off events
    }

    /// <summary>
    /// Prerequisites for unlocking a story.
    /// </summary>
    [System.Serializable]
    public class StoryPrerequisites
    {
        [Tooltip("Story IDs that must be completed")]
        [SerializeField] private List<string> _requiredCompletedStories = new();

        [Tooltip("Quest IDs that must be completed")]
        [SerializeField] private List<string> _requiredCompletedQuests = new();

        [Tooltip("Quest IDs that must be active")]
        [SerializeField] private List<string> _requiredActiveQuests = new();

        [Tooltip("NPC IDs that must have been encountered")]
        [SerializeField] private List<string> _requiredEncounteredNpcs = new();

        [Tooltip("Custom variable conditions")]
        [SerializeField] private List<VariableCondition> _customVariableConditions = new();

        [Tooltip("Minimum chapter number required")]
        [SerializeField] private int _minChapterNumber;

        [Tooltip("Maximum chapter number (0 = no limit)")]
        [SerializeField] private int _maxChapterNumber;

        public IReadOnlyList<string> RequiredCompletedStories => _requiredCompletedStories;
        public IReadOnlyList<string> RequiredCompletedQuests => _requiredCompletedQuests;
        public IReadOnlyList<string> RequiredActiveQuests => _requiredActiveQuests;
        public IReadOnlyList<string> RequiredEncounteredNpcs => _requiredEncounteredNpcs;
        public IReadOnlyList<VariableCondition> CustomVariableConditions => _customVariableConditions;
        public int MinChapterNumber => _minChapterNumber;
        public int MaxChapterNumber => _maxChapterNumber;

        /// <summary>
        /// Checks if there are any prerequisites defined.
        /// </summary>
        public bool HasAnyPrerequisites =>
            _requiredCompletedStories.Count > 0 ||
            _requiredCompletedQuests.Count > 0 ||
            _requiredActiveQuests.Count > 0 ||
            _requiredEncounteredNpcs.Count > 0 ||
            _customVariableConditions.Count > 0 ||
            _minChapterNumber > 0 ||
            _maxChapterNumber > 0;
    }

    /// <summary>
    /// Platform configuration for a story.
    /// </summary>
    [System.Serializable]
    public class StoryPlatformConfig
    {
        [Tooltip("Whether to override the chapter's theme")]
        [SerializeField] private bool _overrideTheme;

        [Tooltip("Theme override for this story's platform")]
        [SerializeField] private LevelTheme _themeOverride = LevelTheme.Forest;

        [Tooltip("Base priority for random selection (higher = more likely)")]
        [SerializeField] private int _basePriority = 50;

        [Tooltip("Whether this story is critical for progression")]
        [SerializeField] private bool _isKeyProgression;

        public bool OverrideTheme => _overrideTheme;
        public LevelTheme ThemeOverride => _themeOverride;
        public int BasePriority => _basePriority;
        public bool IsKeyProgression => _isKeyProgression;
    }

    /// <summary>
    /// Condition for a custom story variable.
    /// </summary>
    [System.Serializable]
    public class VariableCondition
    {
        [Tooltip("Name of the variable to check")]
        [SerializeField] private string _variableName;

        [Tooltip("Type of comparison")]
        [SerializeField] private ComparisonOperator _comparison = ComparisonOperator.Equals;

        [Tooltip("Value to compare against (for numeric or string comparisons)")]
        [SerializeField] private string _compareValue;

        public string VariableName => _variableName;
        public ComparisonOperator Comparison => _comparison;
        public string CompareValue => _compareValue;
    }

    /// <summary>
    /// Comparison operators for variable conditions.
    /// </summary>
    public enum ComparisonOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        IsTrue,
        IsFalse,
        Contains
    }
}
