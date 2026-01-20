using System.Collections.Generic;

namespace Narrative
{
    /// <summary>
    /// Interface for querying and managing story state.
    /// Provides the abstraction layer between game systems and story data.
    /// </summary>
    public interface IStoryStateProvider
    {
        /// <summary>
        /// Gets the current story state.
        /// </summary>
        StoryState CurrentState { get; }

        /// <summary>
        /// Gets the current chapter ID.
        /// </summary>
        string CurrentChapterId { get; }

        /// <summary>
        /// Gets the story requirements for the current story position.
        /// </summary>
        IReadOnlyList<StoryNodeRequirement> GetCurrentRequirements();

        /// <summary>
        /// Gets story requirements for a specific chapter.
        /// </summary>
        IReadOnlyList<StoryNodeRequirement> GetRequirementsForChapter(string chapterId);

        /// <summary>
        /// Gets the next story node requirement (for linear progression).
        /// </summary>
        StoryNodeRequirement GetNextStoryNode();

        /// <summary>
        /// Checks if a specific NPC should appear based on story state.
        /// </summary>
        bool ShouldNpcAppear(string npcId);

        /// <summary>
        /// Gets the dialogue knot for an NPC based on current story state.
        /// </summary>
        string GetNpcDialogueKnot(string npcId);

        /// <summary>
        /// Marks a story node as completed.
        /// </summary>
        void CompleteStoryNode(string nodeId);

        /// <summary>
        /// Records that an NPC was encountered.
        /// </summary>
        void RecordNpcEncounter(string npcId);

        /// <summary>
        /// Starts a quest.
        /// </summary>
        void StartQuest(string questId);

        /// <summary>
        /// Completes a quest.
        /// </summary>
        void CompleteQuest(string questId);

        /// <summary>
        /// Checks if a quest is active.
        /// </summary>
        bool IsQuestActive(string questId);

        /// <summary>
        /// Checks if a quest is completed.
        /// </summary>
        bool IsQuestCompleted(string questId);

        /// <summary>
        /// Advances to the next chapter.
        /// </summary>
        void AdvanceToChapter(string chapterId);

        /// <summary>
        /// Saves the current state.
        /// </summary>
        void SaveState();

        /// <summary>
        /// Loads a previously saved state.
        /// </summary>
        void LoadState(StoryState state);
    }
}
