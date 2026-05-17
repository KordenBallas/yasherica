using System;
using Narrative.Generation;

namespace Narrative
{
    /// <summary>
    /// Interface for binding external C# functions to Ink stories.
    /// Enables Ink scripts to call game logic for combat, quests, rewards, and relationships.
    /// </summary>
    public interface IInkExternalFunctionBinder
    {
        /// <summary>
        /// Binds all external functions to the story manager.
        /// </summary>
        void BindAllExternalFunctions();

        /// <summary>
        /// Enemy count from the last trigger_combat call.
        /// </summary>
        int LastCombatEnemyCount { get; }

        /// <summary>
        /// Quest ID from the last start_quest call.
        /// </summary>
        string LastStartedQuestId { get; }

        /// <summary>
        /// Reward ID from the last grant_reward call.
        /// </summary>
        string LastGrantedRewardId { get; }

        /// <summary>
        /// Event fired when a quest is requested from Ink.
        /// </summary>
        event Action<string> OnQuestRequested;

        /// <summary>
        /// Event fired when a reward is requested from Ink.
        /// </summary>
        event Action<string, int> OnRewardRequested;

        /// <summary>
        /// Event fired when a relationship update is requested from Ink.
        /// </summary>
        event Action<string, int> OnRelationshipUpdateRequested;
    }
}
