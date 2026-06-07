using System;
using Narrative.Dialogue;
using UnityEngine;
using Zenject;

namespace Narrative
{
    /// <summary>
    /// Binds external C# functions to be callable from Ink stories.
    /// Handles combat triggers, quest starts, reward grants, and relationship updates.
    /// </summary>
    public class InkExternalFunctionBinder : IInkExternalFunctionBinder
    {
        private readonly IDialoguePresenter _dialoguePresenter;

        [Inject(Id = "story")]
        private IStoryManager _storyManager;

        private int _lastCombatEnemyCount;
        private string _lastStartedQuestId;
        private string _lastGrantedRewardId;

        public int LastCombatEnemyCount => _lastCombatEnemyCount;
        public string LastStartedQuestId => _lastStartedQuestId;
        public string LastGrantedRewardId => _lastGrantedRewardId;

        public event Action<string> OnQuestRequested;
        public event Action<string, int> OnRewardRequested;
        public event Action<string, int> OnRelationshipUpdateRequested;

        public InkExternalFunctionBinder(IDialoguePresenter dialoguePresenter)
        {
            _dialoguePresenter = dialoguePresenter ?? throw new ArgumentNullException(nameof(dialoguePresenter));
        }

        public void BindAllExternalFunctions()
        {
            if (_storyManager == null)
            {
                Debug.LogWarning("[InkExternalFunctionBinder] No story manager available");
                return;
            }

            _storyManager.BindExternalFunction<int>("trigger_combat", HandleTriggerCombat);
            _storyManager.BindExternalFunction<string>("start_quest", HandleStartQuest);
            _storyManager.BindExternalFunction<string, int, int>("grant_reward", HandleGrantReward);
            _storyManager.BindExternalFunction<string, int>("update_relationship", HandleUpdateRelationship);

            Debug.Log("[InkExternalFunctionBinder] All external functions bound");
        }

        private void HandleTriggerCombat(int enemyCount)
        {
            _lastCombatEnemyCount = enemyCount;
            _dialoguePresenter.FireCombatTriggered(enemyCount.ToString());
        }

        private void HandleStartQuest(string questId)
        {
            _lastStartedQuestId = questId;
            OnQuestRequested?.Invoke(questId);
        }

        private int HandleGrantReward(string rewardId, int amount)
        {
            _lastGrantedRewardId = rewardId;
            OnRewardRequested?.Invoke(rewardId, amount);
            return 1;
        }

        private void HandleUpdateRelationship(string npcId, int delta)
        {
            OnRelationshipUpdateRequested?.Invoke(npcId, delta);
        }
    }
}
