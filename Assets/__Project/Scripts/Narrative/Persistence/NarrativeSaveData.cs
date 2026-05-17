using System;
using System.Collections.Generic;
using Narrative.Generation;

namespace Narrative.Persistence
{
    /// <summary>
    /// Top-level serializable save data for the narrative system.
    /// Contains all persistent narrative state that survives session boundaries.
    /// </summary>
    [Serializable]
    public class NarrativeSaveData
    {
        /// <summary>
        /// Story progress and completion data.
        /// </summary>
        public StoryStateSaveData StoryState;

        /// <summary>
        /// Currently active quests.
        /// </summary>
        public List<QuestSaveData> ActiveQuests = new();

        /// <summary>
        /// Completed quests (for history and prerequisites).
        /// </summary>
        public List<QuestSaveData> CompletedQuests = new();

        /// <summary>
        /// Player's relationships with NPCs.
        /// </summary>
        public List<NpcRelationshipData> NpcRelationships = new();

        /// <summary>
        /// Current Ink story state (JSON from Ink runtime).
        /// Used for resuming mid-dialogue saves.
        /// </summary>
        public string CurrentInkState;

        /// <summary>
        /// ID of the active story session (if any).
        /// </summary>
        public string ActiveSessionId;

        /// <summary>
        /// When this save data was created.
        /// </summary>
        public long SaveTimestamp;

        /// <summary>
        /// Save data version for migration support.
        /// </summary>
        public int Version = 1;
    }

    /// <summary>
    /// Save data for overall story progress.
    /// </summary>
    [Serializable]
    public class StoryStateSaveData
    {
        /// <summary>
        /// Current chapter number.
        /// </summary>
        public int CurrentChapter = 1;

        /// <summary>
        /// IDs of completed stories.
        /// </summary>
        public List<string> CompletedStoryIds = new();

        /// <summary>
        /// Story IDs that have been visited (for tracking visits across sessions).
        /// </summary>
        public List<string> VisitedStoryIds = new();

        /// <summary>
        /// Knots visited within each story (key: storyId, value: list of knot names).
        /// </summary>
        public List<StoryKnotVisitData> StoryKnotVisits = new();

        /// <summary>
        /// IDs of stories currently on cooldown.
        /// </summary>
        public List<StoryCooldownData> StoryCooldowns = new();

        /// <summary>
        /// Player's alignment score (-100 to 100).
        /// </summary>
        public int PlayerAlignment;

        /// <summary>
        /// Faction relationship scores.
        /// </summary>
        public List<FactionRelationshipData> FactionRelationships = new();
    }

    /// <summary>
    /// Tracks which knots have been visited in a specific story.
    /// </summary>
    [Serializable]
    public class StoryKnotVisitData
    {
        public string StoryId;
        public List<string> VisitedKnots = new();
    }

    /// <summary>
    /// Tracks story cooldown state.
    /// </summary>
    [Serializable]
    public class StoryCooldownData
    {
        public string StoryId;
        public int RemainingPlatforms;
    }

    /// <summary>
    /// Tracks faction relationship score.
    /// </summary>
    [Serializable]
    public class FactionRelationshipData
    {
        public string FactionId;
        public int RelationshipScore;
    }

    /// <summary>
    /// Save data for a quest instance.
    /// </summary>
    [Serializable]
    public class QuestSaveData
    {
        /// <summary>
        /// Unique quest identifier.
        /// </summary>
        public string QuestId;

        /// <summary>
        /// Display name of the quest.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Quest description.
        /// </summary>
        public string Description;

        /// <summary>
        /// Story template ID this quest was created from.
        /// </summary>
        public string SourceTemplateId;

        /// <summary>
        /// Current quest status.
        /// </summary>
        public QuestStatus Status;

        /// <summary>
        /// Quest outcome (if completed).
        /// </summary>
        public QuestOutcome Outcome;

        /// <summary>
        /// Objectives with their progress.
        /// </summary>
        public List<QuestObjectiveSaveData> Objectives = new();

        /// <summary>
        /// IDs of NPCs involved in this quest.
        /// </summary>
        public List<string> InvolvedNpcIds = new();

        /// <summary>
        /// Reward snapshots for this quest.
        /// </summary>
        public List<RewardInstanceSnapshot> Rewards = new();

        /// <summary>
        /// When the quest was started (ticks).
        /// </summary>
        public long StartedAtTicks;

        /// <summary>
        /// When the quest was completed (ticks), if applicable.
        /// </summary>
        public long? CompletedAtTicks;

        /// <summary>
        /// Custom data stored on the quest.
        /// </summary>
        public List<CustomDataEntry> CustomData = new();
    }

    /// <summary>
    /// Save data for a quest objective.
    /// </summary>
    [Serializable]
    public class QuestObjectiveSaveData
    {
        public string ObjectiveId;
        public string Description;
        public ObjectiveType ObjectiveType;
        public int CurrentProgress;
        public int RequiredProgress;
        public bool IsRequired;
        public bool IsComplete;
    }

    /// <summary>
    /// Save data for NPC relationship state.
    /// </summary>
    [Serializable]
    public class NpcRelationshipData
    {
        /// <summary>
        /// NPC definition ID (from ScriptableObject).
        /// </summary>
        public string NpcDefinitionId;

        /// <summary>
        /// Relationship score with this NPC (-100 to 100).
        /// </summary>
        public int RelationshipScore;

        /// <summary>
        /// Whether the player has encountered this NPC.
        /// </summary>
        public bool HasBeenEncountered;

        /// <summary>
        /// Story IDs completed with this NPC.
        /// </summary>
        public List<string> CompletedStoryIds = new();

        /// <summary>
        /// Number of times the player has interacted with this NPC.
        /// </summary>
        public int InteractionCount;

        /// <summary>
        /// Last interaction timestamp (ticks).
        /// </summary>
        public long LastInteractionTicks;
    }

    /// <summary>
    /// Generic key-value entry for custom data serialization.
    /// </summary>
    [Serializable]
    public class CustomDataEntry
    {
        public string Key;
        public string Value;
        public string ValueType;
    }
}
