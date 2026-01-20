using System;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Data
{
    /// <summary>
    /// Associates an NPC with a story and defines their role in that story.
    /// Enables NPCs to be story carriers with semantic meaning.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [Serializable]
    public class NpcStoryAssociation
    {
        [Tooltip("The story this NPC is associated with")]
        [SerializeField] private BaseStoryDefinition _story;

        [Tooltip("The NPC's role in this story")]
        [SerializeField] private NpcStoryRole _role = NpcStoryRole.Protagonist;

        [Tooltip("Condition that must be met for this story to trigger")]
        [SerializeField] private StoryTriggerCondition _triggerCondition = new();

        [Tooltip("Priority for this story (higher = more likely to be selected)")]
        [SerializeField] [Range(0, 100)] private int _priority = 50;

        [TextArea(2, 4)]
        [Tooltip("Description of the NPC's involvement (editor only)")]
        [SerializeField] private string _editorNotes;

        public BaseStoryDefinition Story => _story;
        public NpcStoryRole Role => _role;
        public StoryTriggerCondition TriggerCondition => _triggerCondition;
        public int Priority => _priority;
        public string EditorNotes => _editorNotes;

        /// <summary>
        /// Checks if this association has a valid story reference.
        /// </summary>
        public bool IsValid => _story != null;

        /// <summary>
        /// Gets the story ID.
        /// </summary>
        public string StoryId => _story?.StoryId ?? string.Empty;
    }

    /// <summary>
    /// NPC's role in a story.
    /// Defines the semantic relationship between NPC and narrative.
    /// </summary>
    public enum NpcStoryRole
    {
        /// <summary>
        /// Central character in the story.
        /// </summary>
        Protagonist,

        /// <summary>
        /// Opposes the player or creates conflict.
        /// </summary>
        Antagonist,

        /// <summary>
        /// Initiates the story or provides objectives.
        /// </summary>
        QuestGiver,

        /// <summary>
        /// Assists the player during the story.
        /// </summary>
        Companion,

        /// <summary>
        /// Provides information or context.
        /// </summary>
        Witness,

        /// <summary>
        /// Provides services (buying/selling).
        /// </summary>
        Merchant,

        /// <summary>
        /// Provides guidance or advice.
        /// </summary>
        Mentor,

        /// <summary>
        /// Appears briefly in the story.
        /// </summary>
        Cameo,

        /// <summary>
        /// Neutral party or background character.
        /// </summary>
        Bystander
    }

    /// <summary>
    /// Condition for when a story association triggers.
    /// Determines when an NPC's story becomes available.
    /// </summary>
    [Serializable]
    public class StoryTriggerCondition
    {
        [Tooltip("Type of trigger condition")]
        [SerializeField] private TriggerType _triggerType = TriggerType.Always;

        [Tooltip("Number of encounters before this story becomes available")]
        [SerializeField] private int _requiredEncounters = 0;

        [Tooltip("Quest that must be active")]
        [SerializeField] private string _requiredActiveQuest;

        [Tooltip("Quest that must be completed")]
        [SerializeField] private string _requiredCompletedQuest;

        [Tooltip("Story that must be completed")]
        [SerializeField] private string _requiredCompletedStory;

        [Tooltip("Minimum chapter number")]
        [SerializeField] private int _minChapterNumber;

        [Tooltip("Custom variable name to check")]
        [SerializeField] private string _customVariableName;

        [Tooltip("Expected value of custom variable")]
        [SerializeField] private string _customVariableValue;

        public TriggerType TriggerType => _triggerType;
        public int RequiredEncounters => _requiredEncounters;
        public string RequiredActiveQuest => _requiredActiveQuest;
        public string RequiredCompletedQuest => _requiredCompletedQuest;
        public string RequiredCompletedStory => _requiredCompletedStory;
        public int MinChapterNumber => _minChapterNumber;
        public string CustomVariableName => _customVariableName;
        public string CustomVariableValue => _customVariableValue;

        /// <summary>
        /// Checks if this condition has any requirements.
        /// </summary>
        public bool HasCondition => _triggerType != TriggerType.Always;
    }

    /// <summary>
    /// Type of trigger condition for NPC stories.
    /// </summary>
    public enum TriggerType
    {
        /// <summary>
        /// Story is always available when NPC is encountered.
        /// </summary>
        Always,

        /// <summary>
        /// Story becomes available after N encounters with this NPC.
        /// </summary>
        AfterEncounters,

        /// <summary>
        /// Story becomes available when a specific quest is active.
        /// </summary>
        QuestActive,

        /// <summary>
        /// Story becomes available when a specific quest is completed.
        /// </summary>
        QuestCompleted,

        /// <summary>
        /// Story becomes available when another story is completed.
        /// </summary>
        StoryCompleted,

        /// <summary>
        /// Story becomes available at a specific chapter.
        /// </summary>
        ChapterReached,

        /// <summary>
        /// Story becomes available when a custom variable matches.
        /// </summary>
        CustomVariable
    }
}
