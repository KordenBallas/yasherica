using System;
using Narrative.Dialogue;
using Narrative.Generation;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// Binds external C# functions to be callable from Ink stories.
    /// Handles combat triggers, quest starts, reward grants, and relationship updates.
    /// </summary>
    public class InkExternalFunctionBinder : IInkExternalFunctionBinder
    {
        private readonly IStoryManager _storyManager;
        private readonly IDialoguePresenter _dialoguePresenter;
        private readonly IQuestManager _questManager;

        private int _lastCombatEnemyCount;
        private string _lastStartedQuestId;
        private string _lastGrantedRewardId;

        public int LastCombatEnemyCount => _lastCombatEnemyCount;
        public string LastStartedQuestId => _lastStartedQuestId;
        public string LastGrantedRewardId => _lastGrantedRewardId;

        public event Action<string> OnQuestRequested;
        public event Action<string, int> OnRewardRequested;
        public event Action<string, int> OnRelationshipUpdateRequested;

        public InkExternalFunctionBinder(
            IStoryManager storyManager,
            IDialoguePresenter dialoguePresenter,
            IQuestManager questManager = null)
        {
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
            _dialoguePresenter = dialoguePresenter ?? throw new ArgumentNullException(nameof(dialoguePresenter));
            _questManager = questManager;
        }

        public void BindAllExternalFunctions()
        {
            BindTriggerCombat();
            BindStartQuest();
            BindGrantReward();
            BindUpdateRelationship();

            Debug.Log("[InkExternalFunctionBinder] All external functions bound");
        }

        private void BindTriggerCombat()
        {
            _storyManager.BindExternalFunction<int>("trigger_combat", HandleTriggerCombat);
            Debug.Log("[InkExternalFunctionBinder] Bound: trigger_combat(int)");
        }

        private void BindStartQuest()
        {
            _storyManager.BindExternalFunction<string>("start_quest", HandleStartQuest);
            Debug.Log("[InkExternalFunctionBinder] Bound: start_quest(string)");
        }

        private void BindGrantReward()
        {
            _storyManager.BindExternalFunction<string, int, int>("grant_reward", HandleGrantReward);
            Debug.Log("[InkExternalFunctionBinder] Bound: grant_reward(string, int)");
        }

        private void BindUpdateRelationship()
        {
            _storyManager.BindExternalFunction<string, int>("update_relationship", HandleUpdateRelationship);
            Debug.Log("[InkExternalFunctionBinder] Bound: update_relationship(string, int)");
        }

        private void HandleTriggerCombat(int enemyCount)
        {
            _lastCombatEnemyCount = enemyCount;
            Debug.Log($"[InkExternalFunctionBinder] trigger_combat called: enemyCount={enemyCount}");
            _dialoguePresenter.FireCombatTriggered(enemyCount.ToString());
        }

        private void HandleStartQuest(string questId)
        {
            _lastStartedQuestId = questId;
            Debug.Log($"[InkExternalFunctionBinder] start_quest called: questId={questId}");

            // Try to start the quest if QuestManager is available
            if (_questManager != null && _questManager.IsQuestActive(questId))
            {
                // Quest exists but may not be started yet - this is handled elsewhere
                Debug.Log($"[InkExternalFunctionBinder] Quest '{questId}' exists in active quests");
            }

            OnQuestRequested?.Invoke(questId);
        }

        private int HandleGrantReward(string rewardId, int amount)
        {
            _lastGrantedRewardId = rewardId;
            Debug.Log($"[InkExternalFunctionBinder] grant_reward called: rewardId={rewardId}, amount={amount}");

            OnRewardRequested?.Invoke(rewardId, amount);

            // Return 1 for success (can be used in Ink for conditional logic)
            return 1;
        }

        private void HandleUpdateRelationship(string npcId, int delta)
        {
            Debug.Log($"[InkExternalFunctionBinder] update_relationship called: npcId={npcId}, delta={delta}");

            OnRelationshipUpdateRequested?.Invoke(npcId, delta);
        }
    }
}
