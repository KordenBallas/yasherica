using System;

namespace Narrative
{
    /// <summary>
    /// Interface for binding external C# functions to Ink stories.
    /// Enables Ink scripts to call game logic for combat, quests, rewards, and relationships.
    /// </summary>
    public interface IInkExternalFunctionBinder
    {
        /// <summary>
        /// Binds external functions to the story manager.
        /// Must be called AFTER LoadStory() on the story manager.
        /// </summary>
        void BindToStoryManager();

        /// <summary>
        /// Binds external functions to the NPC manager.
        /// Must be called AFTER LoadStory() on the NPC manager.
        /// </summary>
        void BindToNpcManager();

        /// <summary>
        /// Binds all external functions to both story managers (quest and NPC).
        /// Convenience method that calls both BindToStoryManager and BindToNpcManager.
        /// </summary>
        void BindAllExternalFunctions();

        int LastCombatEnemyCount { get; }
        string LastStartedQuestId { get; }
        string LastGrantedRewardId { get; }

        event Action<string> OnQuestRequested;
        event Action<string, int> OnRewardRequested;
        event Action<string, int> OnRelationshipUpdateRequested;
        event Action<string> OnCombatRequested;
    }
}
