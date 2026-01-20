using System;
using UnityEngine;

namespace Narrative.Data
{
    /// <summary>
    /// Defines a relationship between two stories in the story graph.
    /// Represents a directed edge from one story to another.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [Serializable]
    public class StoryRelationship
    {
        [Tooltip("Type of relationship between stories")]
        [SerializeField] private StoryRelationshipType _relationshipType;

        [Tooltip("Target story this relationship points to")]
        [SerializeField] private Definitions.BaseStoryDefinition _targetStory;

        [Tooltip("Condition that must be met for this relationship to activate")]
        [SerializeField] private StoryRelationshipCondition _condition = new();

        [Tooltip("Weight for prioritization (higher = more important)")]
        [SerializeField] [Range(0f, 1f)] private float _weight = 0.5f;

        [Tooltip("Whether this relationship is critical for story progression")]
        [SerializeField] private bool _isKeyProgression;

        [TextArea(2, 4)]
        [Tooltip("Description of why this relationship exists (editor only)")]
        [SerializeField] private string _editorNotes;

        public StoryRelationshipType RelationshipType => _relationshipType;
        public Definitions.BaseStoryDefinition TargetStory => _targetStory;
        public StoryRelationshipCondition Condition => _condition;
        public float Weight => _weight;
        public bool IsKeyProgression => _isKeyProgression;
        public string EditorNotes => _editorNotes;
    }

    /// <summary>
    /// Type of relationship between stories.
    /// Defines how stories connect and unlock in the narrative graph.
    /// </summary>
    public enum StoryRelationshipType
    {
        /// <summary>
        /// Story B unlocks after completing Story A (linear progression).
        /// Example: Chapter 1 → Chapter 2
        /// </summary>
        Sequence,

        /// <summary>
        /// Player choice leads to one of multiple stories (branching narrative).
        /// Example: "Help the merchant" → Story B or "Ignore the merchant" → Story C
        /// </summary>
        Branch,

        /// <summary>
        /// Story B requires Story A to be completed before it becomes available.
        /// Example: "Advanced Training" requires "Basic Training"
        /// </summary>
        Prerequisite,

        /// <summary>
        /// Stories are available at the same time (parallel content).
        /// Example: Multiple side quests available in the same chapter
        /// </summary>
        Parallel,

        /// <summary>
        /// Completing one story locks out the other (mutually exclusive).
        /// Example: "Join the rebels" excludes "Join the empire"
        /// </summary>
        Exclusive,

        /// <summary>
        /// Specific outcome/choice in Story A triggers Story B.
        /// Example: "Betrayed the merchant" → "Merchant's Revenge"
        /// </summary>
        Consequence,

        /// <summary>
        /// Story B references or continues themes from Story A (narrative continuity).
        /// Example: NPC remembers previous interaction
        /// </summary>
        Callback,

        /// <summary>
        /// Stories are connected by matching attributes (dynamic connection).
        /// Example: All stories with "betrayal" theme
        /// </summary>
        TagMatch
    }

    /// <summary>
    /// Condition that must be met for a story relationship to activate.
    /// </summary>
    [Serializable]
    public class StoryRelationshipCondition
    {
        [Tooltip("Type of condition to check")]
        [SerializeField] private ConditionType _conditionType = ConditionType.None;

        [Tooltip("Ink variable name to check (for InkVariable condition)")]
        [SerializeField] private string _inkVariableName;

        [Tooltip("Expected value of the Ink variable")]
        [SerializeField] private string _expectedValue;

        [Tooltip("Quest ID that must be completed (for QuestCompleted condition)")]
        [SerializeField] private string _requiredQuestId;

        [Tooltip("NPC ID that must be encountered (for NpcEncountered condition)")]
        [SerializeField] private string _requiredNpcId;

        [Tooltip("Minimum chapter number (for ChapterNumber condition)")]
        [SerializeField] private int _minChapterNumber;

        public ConditionType ConditionType => _conditionType;
        public string InkVariableName => _inkVariableName;
        public string ExpectedValue => _expectedValue;
        public string RequiredQuestId => _requiredQuestId;
        public string RequiredNpcId => _requiredNpcId;
        public int MinChapterNumber => _minChapterNumber;

        /// <summary>
        /// Checks if this condition has any requirements.
        /// </summary>
        public bool HasCondition => _conditionType != ConditionType.None;
    }

    /// <summary>
    /// Type of condition for story relationship activation.
    /// </summary>
    public enum ConditionType
    {
        /// <summary>
        /// No condition - relationship is always active after source story completes.
        /// </summary>
        None,

        /// <summary>
        /// Relationship activates if an Ink variable has a specific value.
        /// </summary>
        InkVariable,

        /// <summary>
        /// Relationship activates if a specific quest is completed.
        /// </summary>
        QuestCompleted,

        /// <summary>
        /// Relationship activates if an NPC has been encountered.
        /// </summary>
        NpcEncountered,

        /// <summary>
        /// Relationship activates if player has reached a minimum chapter number.
        /// </summary>
        ChapterNumber,

        /// <summary>
        /// Relationship activates based on custom condition evaluated at runtime.
        /// </summary>
        Custom
    }
}
