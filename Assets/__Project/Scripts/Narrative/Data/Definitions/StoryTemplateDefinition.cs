using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for parameterized story templates.
    /// Templates define Ink stories with parameter slots that get bound at runtime.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryTemplateDefinition", menuName = "Narrative/Story/Story Template")]
    public class StoryTemplateDefinition : BaseStoryDefinition
    {
        [Header("Template Configuration")]
        [Tooltip("Platform type where this template can appear")]
        [SerializeField] private StoryPlatformType _platformType = StoryPlatformType.Dialogue;

        [Tooltip("Whether this template can be repeated")]
        [SerializeField] private bool _isRepeatable;

        [Tooltip("Minimum platforms between uses if repeatable")]
        [SerializeField] private int _cooldownPlatforms = 5;

        [Header("Parameter Slots")]
        [Tooltip("NPC parameter slots that need to be filled")]
        [SerializeField] private List<TemplateParameterSlot> _parameterSlots = new();

        [Header("Reward Configuration")]
        [Tooltip("Possible rewards for this story template")]
        [SerializeField] private List<TemplateRewardSlot> _rewardSlots = new();

        [Header("Context Requirements")]
        [Tooltip("Required theme for this template")]
        [SerializeField] private bool _requiresSpecificTheme;

        [SerializeField] private LevelTheme _requiredTheme;

        [Tooltip("Minimum chapter for this template")]
        [SerializeField] private int _minimumChapter;

        [Tooltip("Maximum chapter for this template (0 = no limit)")]
        [SerializeField] private int _maximumChapter;

        [Tooltip("Required player alignment range (-100 to 100)")]
        [SerializeField] private Vector2Int _alignmentRange = new(-100, 100);

        // Public read-only accessors
        public StoryPlatformType PlatformType => _platformType;
        public bool IsRepeatable => _isRepeatable;
        public int CooldownPlatforms => _cooldownPlatforms;
        public IReadOnlyList<TemplateParameterSlot> ParameterSlots => _parameterSlots;
        public IReadOnlyList<TemplateRewardSlot> RewardSlots => _rewardSlots;
        public bool RequiresSpecificTheme => _requiresSpecificTheme;
        public LevelTheme RequiredTheme => _requiredTheme;
        public int MinimumChapter => _minimumChapter;
        public int MaximumChapter => _maximumChapter;
        public Vector2Int AlignmentRange => _alignmentRange;

        /// <summary>
        /// Checks if this template has any NPC parameter slots.
        /// </summary>
        public bool HasNpcSlots
        {
            get
            {
                foreach (var slot in _parameterSlots)
                {
                    if (slot.ParameterType == ParameterType.Npc)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the count of required NPC slots.
        /// </summary>
        public int RequiredNpcCount
        {
            get
            {
                int count = 0;
                foreach (var slot in _parameterSlots)
                {
                    if (slot.ParameterType == ParameterType.Npc && slot.IsRequired)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Returns the configured platform type.
        /// </summary>
        public override StoryPlatformType GetPlatformType()
        {
            return _platformType;
        }

        /// <summary>
        /// Templates can be repeated based on configuration.
        /// </summary>
        public override bool CanRepeat()
        {
            return _isRepeatable;
        }

        private void OnValidate()
        {
            // Auto-set StoryType for templates
            _storyType = StoryType.SideStory;

            // Ensure cooldown is reasonable
            if (_cooldownPlatforms < 0)
                _cooldownPlatforms = 0;

            // Ensure chapter range is valid
            if (_maximumChapter > 0 && _minimumChapter > _maximumChapter)
                _minimumChapter = _maximumChapter;

            // Ensure alignment range is valid
            _alignmentRange.x = Mathf.Clamp(_alignmentRange.x, -100, 100);
            _alignmentRange.y = Mathf.Clamp(_alignmentRange.y, -100, 100);
            if (_alignmentRange.x > _alignmentRange.y)
                _alignmentRange.x = _alignmentRange.y;
        }
    }

    /// <summary>
    /// Defines a parameter slot in a story template.
    /// </summary>
    [System.Serializable]
    public class TemplateParameterSlot
    {
        [Tooltip("Unique identifier for this parameter")]
        [SerializeField] private string _parameterId;

        [Tooltip("Type of parameter")]
        [SerializeField] private ParameterType _parameterType = ParameterType.Npc;

        [Tooltip("Display name for UI")]
        [SerializeField] private string _displayName;

        [Tooltip("Ink variable name to bind to")]
        [SerializeField] private string _inkVariableName;

        [Tooltip("Whether this parameter must be filled")]
        [SerializeField] private bool _isRequired = true;

        [Header("NPC Constraints")]
        [Tooltip("Required faction for NPC parameters")]
        [SerializeField] private NpcFaction _requiredFaction = NpcFaction.Neutral;

        [Tooltip("Whether faction is enforced")]
        [SerializeField] private bool _enforceFaction;

        [Tooltip("Required NPC role")]
        [SerializeField] private Generation.NpcRole _requiredRole = Generation.NpcRole.None;

        [Tooltip("Required traits (any match)")]
        [SerializeField] private List<string> _requiredTraits = new();

        [Tooltip("Specific NPC ID if hardcoded")]
        [SerializeField] private string _specificNpcId;

        [Header("Location Constraints")]
        [Tooltip("Required location type")]
        [SerializeField] private LocationType _locationType = LocationType.Any;

        [Header("Reward Constraints")]
        [Tooltip("Reward type for reward parameters")]
        [SerializeField] private RewardType _rewardType = RewardType.Currency;

        [Tooltip("Value range for rewards")]
        [SerializeField] private Vector2Int _rewardValueRange = new(10, 50);

        // Public accessors
        public string ParameterId => _parameterId;
        public ParameterType ParameterType => _parameterType;
        public string DisplayName => _displayName;
        public string InkVariableName => _inkVariableName;
        public bool IsRequired => _isRequired;
        public NpcFaction RequiredFaction => _requiredFaction;
        public bool EnforceFaction => _enforceFaction;
        public Generation.NpcRole RequiredRole => _requiredRole;
        public IReadOnlyList<string> RequiredTraits => _requiredTraits;
        public string SpecificNpcId => _specificNpcId;
        public LocationType LocationType => _locationType;
        public RewardType RewardType => _rewardType;
        public Vector2Int RewardValueRange => _rewardValueRange;

        /// <summary>
        /// Whether this slot targets a specific NPC.
        /// </summary>
        public bool HasSpecificNpc => !string.IsNullOrEmpty(_specificNpcId);
    }

    /// <summary>
    /// Defines a reward slot in a story template.
    /// </summary>
    [System.Serializable]
    public class TemplateRewardSlot
    {
        [Tooltip("Reward definition to use")]
        [SerializeField] private RewardDefinition _rewardDefinition;

        [Tooltip("Probability of this reward appearing (0-1)")]
        [SerializeField] private float _probability = 1f;

        [Tooltip("Multiplier for reward value")]
        [SerializeField] private float _valueMultiplier = 1f;

        [Tooltip("Condition for this reward")]
        [SerializeField] private RewardCondition _condition = RewardCondition.Always;

        public RewardDefinition RewardDefinition => _rewardDefinition;
        public float Probability => _probability;
        public float ValueMultiplier => _valueMultiplier;
        public RewardCondition Condition => _condition;
    }

    /// <summary>
    /// Type of parameter in a template slot.
    /// </summary>
    public enum ParameterType
    {
        Npc,
        Location,
        Reward,
        ItemName,
        Quantity,
        Custom
    }

    /// <summary>
    /// Type of location for location parameters.
    /// </summary>
    public enum LocationType
    {
        Any,
        Town,
        Wilderness,
        Dungeon,
        Shop,
        Landmark
    }

    /// <summary>
    /// Condition for reward availability.
    /// </summary>
    public enum RewardCondition
    {
        Always,
        OnSuccess,
        OnPartialSuccess,
        OnPeacefulResolution,
        OnCombatVictory
    }
}
