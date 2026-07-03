using System.Collections;
using Combat.Controller;
using Combat.Core;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Player
{
    /// <summary>
    /// Drives the Resolve phase with visual pacing: when the round enters EnemyResolve,
    /// fires the controller's committed enemy intents one by one with short delays so the
    /// player can read each blow landing. Planning is synchronous inside the controller;
    /// this class only owns the paced resolution coroutine.
    /// </summary>
    public class EnemyRoundController
    {
        private const float DelayBeforeFirstIntentSeconds = 0.5f;
        private const float DelayBetweenIntentsSeconds = 0.4f;

        private ICombatController _combatController;
        private MonoBehaviour _coroutineRunner;
        [Inject] private IGameLogger _logger;

        /// <summary>
        /// Initializes the controller. Requires the actual combat controller instance and a
        /// MonoBehaviour to run the pacing coroutine.
        /// </summary>
        public void Initialize(ICombatController combatController, MonoBehaviour coroutineRunner)
        {
            _combatController = combatController;
            _coroutineRunner = coroutineRunner;
            _combatController.OnRoundPhaseChanged += OnRoundPhaseChanged;
            _logger.Info(LogCategory.Combat,"[EnemyRoundController] Initialized and subscribed to OnRoundPhaseChanged");
        }

        /// <summary>
        /// Cleanup - unsubscribe from events.
        /// </summary>
        public void Dispose()
        {
            if (_combatController != null)
            {
                _combatController.OnRoundPhaseChanged -= OnRoundPhaseChanged;
                _logger.Info(LogCategory.Combat,"[EnemyRoundController] Disposed and unsubscribed");
            }
        }

        private void OnRoundPhaseChanged(RoundPhase phase)
        {
            if (phase != RoundPhase.EnemyResolve)
                return;

            if (_coroutineRunner == null)
            {
                _logger.Error(LogCategory.Combat,"[EnemyRoundController] Cannot resolve enemy intents - no coroutine runner available!");
                return;
            }

            _coroutineRunner.StartCoroutine(ResolveIntents());
        }

        private IEnumerator ResolveIntents()
        {
            _logger.Info(LogCategory.Combat,"[EnemyRoundController] Resolve phase started");
            yield return new WaitForSeconds(DelayBeforeFirstIntentSeconds);

            while (_combatController.ResolveNextEnemyIntent())
            {
                yield return new WaitForSeconds(DelayBetweenIntentsSeconds);
            }

            _logger.Info(LogCategory.Combat,"[EnemyRoundController] Resolve phase completed");
        }
    }
}
