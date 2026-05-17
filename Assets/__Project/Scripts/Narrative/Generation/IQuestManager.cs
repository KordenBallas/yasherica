using System;
using System.Collections.Generic;

namespace Narrative.Generation
{
    /// <summary>
    /// Interface for creating and tracking QuestInstances.
    /// Manages the lifecycle of quests from creation to completion.
    /// </summary>
    public interface IQuestManager
    {
        /// <summary>
        /// Event fired when a quest is started.
        /// </summary>
        event Action<QuestInstance> OnQuestStarted;

        /// <summary>
        /// Event fired when a quest is completed.
        /// </summary>
        event Action<QuestInstance, QuestOutcome> OnQuestCompleted;

        /// <summary>
        /// Event fired when a quest objective is updated.
        /// </summary>
        event Action<QuestInstance, QuestObjective> OnObjectiveUpdated;

        /// <summary>
        /// Event fired when a quest is failed.
        /// </summary>
        event Action<QuestInstance> OnQuestFailed;

        /// <summary>
        /// Gets all active quests.
        /// </summary>
        IReadOnlyList<QuestInstance> ActiveQuests { get; }

        /// <summary>
        /// Gets all completed quests.
        /// </summary>
        IReadOnlyList<QuestInstance> CompletedQuests { get; }

        /// <summary>
        /// Creates a new quest from a bound story.
        /// </summary>
        /// <param name="boundStory">The bound story to create a quest from</param>
        /// <returns>The created quest instance</returns>
        QuestInstance CreateQuest(BoundStory boundStory);

        /// <summary>
        /// Starts a quest by ID.
        /// </summary>
        /// <param name="questId">The quest ID to start</param>
        /// <returns>True if quest was started successfully</returns>
        bool StartQuest(string questId);

        /// <summary>
        /// Completes a quest with the given outcome.
        /// </summary>
        /// <param name="questId">The quest ID to complete</param>
        /// <param name="outcome">The outcome of the quest</param>
        void CompleteQuest(string questId, QuestOutcome outcome);

        /// <summary>
        /// Fails a quest.
        /// </summary>
        /// <param name="questId">The quest ID to fail</param>
        void FailQuest(string questId);

        /// <summary>
        /// Updates an objective within a quest.
        /// </summary>
        /// <param name="questId">The quest ID</param>
        /// <param name="objectiveId">The objective ID to update</param>
        /// <param name="progress">New progress value</param>
        void UpdateObjective(string questId, string objectiveId, int progress);

        /// <summary>
        /// Completes an objective within a quest.
        /// </summary>
        /// <param name="questId">The quest ID</param>
        /// <param name="objectiveId">The objective ID to complete</param>
        void CompleteObjective(string questId, string objectiveId);

        /// <summary>
        /// Gets a quest by ID.
        /// </summary>
        /// <param name="questId">The quest ID</param>
        /// <returns>The quest instance or null if not found</returns>
        QuestInstance GetQuest(string questId);

        /// <summary>
        /// Checks if a quest is active.
        /// </summary>
        /// <param name="questId">The quest ID to check</param>
        /// <returns>True if the quest is active</returns>
        bool IsQuestActive(string questId);

        /// <summary>
        /// Checks if a quest is completed.
        /// </summary>
        /// <param name="questId">The quest ID to check</param>
        /// <returns>True if the quest is completed</returns>
        bool IsQuestCompleted(string questId);

        /// <summary>
        /// Gets quests involving a specific NPC.
        /// </summary>
        /// <param name="npcId">The NPC ID to search for</param>
        /// <returns>Quests involving this NPC</returns>
        IReadOnlyList<QuestInstance> GetQuestsForNpc(string npcId);

        /// <summary>
        /// Gets quests associated with a bound story.
        /// </summary>
        /// <param name="boundStory">The bound story to search for</param>
        /// <returns>Quests linked to this bound story</returns>
        IReadOnlyList<QuestInstance> GetQuestsForBoundStory(BoundStory boundStory);

        /// <summary>
        /// Handles dialogue completion for quest tracking.
        /// Updates quest objectives and potentially completes quests.
        /// </summary>
        /// <param name="boundStory">The bound story that dialogue belongs to</param>
        /// <param name="outcome">The dialogue outcome</param>
        void OnDialogueCompleted(BoundStory boundStory, Narrative.DialogueOutcomeType outcome);

        /// <summary>
        /// Evaluates and grants rewards for a completed quest.
        /// </summary>
        /// <param name="quest">The quest to evaluate</param>
        /// <param name="outcome">The quest outcome</param>
        void EvaluateRewards(QuestInstance quest, QuestOutcome outcome);
    }

    /// <summary>
    /// Outcome of a completed quest.
    /// </summary>
    public enum QuestOutcome
    {
        Success,
        PartialSuccess,
        Failure,
        Abandoned
    }
}
