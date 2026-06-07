using System;

namespace Narrative
{
    /// <summary>
    /// Interface for binding external C# functions to Ink stories.
    /// Enables Ink scripts to call game logic for combat, quests, rewards, and relationships.
    /// </summary>
    public interface IInkExternalFunctionBinder
    {
        void BindAllExternalFunctions();

        int LastCombatEnemyCount { get; }
        string LastStartedQuestId { get; }
        string LastGrantedRewardId { get; }

        event Action<string> OnQuestRequested;
        event Action<string, int> OnRewardRequested;
        event Action<string, int> OnRelationshipUpdateRequested;
    }
}
