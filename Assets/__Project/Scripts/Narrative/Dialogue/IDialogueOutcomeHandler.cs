using System;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Interface for handling dialogue outcomes.
    /// Responsible for quest completion, NPC state updates, relationship changes, and story recording.
    /// </summary>
    public interface IDialogueOutcomeHandler
    {
        /// <summary>
        /// Event fired when a quest should be started.
        /// </summary>
        event Action<string> OnQuestStart;

        /// <summary>
        /// Event fired when a quest is completed.
        /// </summary>
        event Action<string, DialogueOutcomeType> OnQuestCompleted;

        /// <summary>
        /// Event fired when NPC relationship changes.
        /// </summary>
        event Action<string, int> OnRelationshipChanged;

        /// <summary>
        /// Handles a dialogue outcome for the given context.
        /// </summary>
        /// <param name="outcome">The outcome type</param>
        /// <param name="context">The dialogue context</param>
        void HandleOutcome(DialogueOutcomeType outcome, IDialogueContext context);

        /// <summary>
        /// Records a quest start from dialogue.
        /// </summary>
        /// <param name="questId">The quest ID to start</param>
        void StartQuest(string questId);

        /// <summary>
        /// Records story node completion.
        /// </summary>
        /// <param name="nodeId">The node ID to complete</param>
        void CompleteStoryNode(string nodeId);

        /// <summary>
        /// Updates NPC relationship.
        /// </summary>
        /// <param name="npcId">The NPC ID</param>
        /// <param name="delta">Relationship change</param>
        void UpdateRelationship(string npcId, int delta);
    }
}
