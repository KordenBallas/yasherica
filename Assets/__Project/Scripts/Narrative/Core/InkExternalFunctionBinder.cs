using Narrative.Dialogue;
using UnityEngine;

namespace Narrative
{
    public class InkExternalFunctionBinder : IInkExternalFunctionBinder
    {
        private readonly IStoryManager _storyManager;
        private readonly DialoguePresenter _dialoguePresenter;
        private int _lastCombatEnemyCount;

        public int LastCombatEnemyCount => _lastCombatEnemyCount;

        public InkExternalFunctionBinder(
            IStoryManager storyManager,
            DialoguePresenter dialoguePresenter)
        {
            _storyManager = storyManager;
            _dialoguePresenter = dialoguePresenter;
        }

        public void BindAllExternalFunctions()
        {
            BindTriggerCombat();
        }

        private void BindTriggerCombat()
        {
            _storyManager.BindExternalFunction<int>("trigger_combat", HandleTriggerCombat);
            Debug.Log("[InkExternalFunctionBinder] Bound: trigger_combat(int)");
        }

        private void HandleTriggerCombat(int enemyCount)
        {
            _lastCombatEnemyCount = enemyCount;
            Debug.Log($"[InkExternalFunctionBinder] trigger_combat called: enemyCount={enemyCount}");
            _dialoguePresenter.FireCombatTriggered(enemyCount.ToString());
        }
    }
}
