using System;
using UnityEngine;
using Zenject;

namespace Narrative
{
    /// <summary>
    /// Binds external C# functions to be callable from Ink stories.
    /// Binds to both the "story" manager (quest/encounter Ink) and "npc" manager (character Ink).
    /// Handles combat triggers, quest starts, reward grants, and relationship updates.
    /// </summary>
    public class InkExternalFunctionBinder : IInkExternalFunctionBinder
    {
        [Inject(Id = "story")]
        private IStoryManager _storyManager;

        [Inject(Id = "npc")]
        private IStoryManager _npcManager;

        private int _lastCombatEnemyCount;
        private string _lastStartedQuestId;
        private string _lastGrantedRewardId;

        public int LastCombatEnemyCount => _lastCombatEnemyCount;
        public string LastStartedQuestId => _lastStartedQuestId;
        public string LastGrantedRewardId => _lastGrantedRewardId;

        public event Action<string> OnQuestRequested;
        public event Action<string, int> OnRewardRequested;
        public event Action<string, int> OnRelationshipUpdateRequested;
        public event Action<string> OnCombatRequested;

        public void BindToStoryManager()
        {
            BindToManager(_storyManager, "story");
        }

        public void BindToNpcManager()
        {
            BindToManager(_npcManager, "npc");
        }

        public void BindAllExternalFunctions()
        {
            // Bind to story manager (quest/encounter Ink)
            BindToStoryManager();

            // Bind to NPC manager (character Ink)
            BindToNpcManager();

            Debug.Log("[InkExternalFunctionBinder] All external functions bound to both managers");
        }

        private void BindToManager(IStoryManager manager, string managerId)
        {
            if (manager == null)
            {
                Debug.LogWarning($"[InkExternalFunctionBinder] No {managerId} manager available");
                return;
            }

            manager.BindExternalFunction<int>("trigger_combat", HandleTriggerCombat);
            manager.BindExternalFunction<string>("start_quest", HandleStartQuest);
            manager.BindExternalFunction<string, int, int>("grant_reward", HandleGrantReward);
            manager.BindExternalFunction<string, int>("update_relationship", HandleUpdateRelationship);

            Debug.Log($"[InkExternalFunctionBinder] External functions bound to {managerId} manager");
        }

        private void HandleTriggerCombat(int enemyCount)
        {
            _lastCombatEnemyCount = enemyCount;
            OnCombatRequested?.Invoke(enemyCount.ToString());
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
