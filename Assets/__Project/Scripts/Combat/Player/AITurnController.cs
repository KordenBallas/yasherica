using System.Collections;
using Combat.Controller;
using Combat.Core;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Player
{
    /// <summary>
    /// Controls AI player turns.
    /// Subscribes to OnTurnStarted event and executes actions for AI players.
    /// Pure C# service with no MonoBehaviour dependencies in logic.
    /// </summary>
    public class AITurnController
    {
        private ICombatController _combatController;
        private MonoBehaviour _coroutineRunner;
        [Inject] private IGameLogger _logger;

        public AITurnController()
        {
        }

        /// <summary>
        /// Initializes the AI turn controller.
        /// Requires the actual combat controller instance and a MonoBehaviour to run coroutines.
        /// </summary>
        public void Initialize(ICombatController combatController, MonoBehaviour coroutineRunner)
        {
            _combatController = combatController;
            _coroutineRunner = coroutineRunner;
            _combatController.OnTurnStarted += OnTurnStarted;
            _logger.Info(LogCategory.Combat,"[AITurnController] Initialized and subscribed to OnTurnStarted");
        }

        /// <summary>
        /// Cleanup - unsubscribe from events.
        /// </summary>
        public void Dispose()
        {
            if (_combatController != null)
            {
                _combatController.OnTurnStarted -= OnTurnStarted;
                _logger.Info(LogCategory.Combat,"[AITurnController] Disposed and unsubscribed from OnTurnStarted");
            }
        }

        private void OnTurnStarted(IPlayer player)
        {
            _logger.Info(LogCategory.Combat,$"[AITurnController] OnTurnStarted - Player: {player.Name} (Type: {player.Type})");

            if (player.Type == PlayerType.AI)
            {
                _logger.Info(LogCategory.Combat,$"[AITurnController] AI player's turn - starting ProcessAITurn coroutine");
                if (_coroutineRunner != null)
                {
                    _coroutineRunner.StartCoroutine(ProcessAITurn(player));
                }
                else
                {
                    _logger.Error(LogCategory.Combat,"[AITurnController] Cannot process AI turn - no coroutine runner available!");
                }
            }
            else
            {
                _logger.Info(LogCategory.Combat,$"[AITurnController] Human player's turn - no AI processing needed");
            }
        }

        private IEnumerator ProcessAITurn(IPlayer aiPlayer)
        {
            _logger.Info(LogCategory.Combat,$"[AITurnController] ProcessAITurn started for {aiPlayer.Name}");

            var ai = aiPlayer as AIPlayer;
            if (ai == null)
            {
                _logger.Error(LogCategory.Combat,$"[AITurnController] Player {aiPlayer.Name} is not an AIPlayer!");
                yield break;
            }

            // Get all active units for this AI player
            var activeUnits = _combatController.CombatState.GetActiveUnitsByPlayer(aiPlayer);
            _logger.Info(LogCategory.Combat,$"[AITurnController] AI has {activeUnits.Count} active unit(s)");

            foreach (var unit in activeUnits)
            {
                _logger.Info(LogCategory.Combat,$"[AITurnController] Processing AI unit {unit.Id}");

                // Wait a bit for visual feedback
                yield return new WaitForSeconds(0.5f);

                // Request action from AI decision maker
                var action = ai.RequestAction(_combatController.CombatState, unit);

                if (action != null)
                {
                    _logger.Info(LogCategory.Combat,$"[AITurnController] AI decided action: {action.Type} for unit {unit.Id}");

                    // Execute the action
                    var result = _combatController.ProcessAction(action);

                    if (result.Success)
                    {
                        _logger.Info(LogCategory.Combat,$"[AITurnController] AI action executed successfully");
                    }
                    else
                    {
                        _logger.Warning(LogCategory.Combat,$"[AITurnController] AI action failed: {result.ErrorMessage}");
                    }
                }
                else
                {
                    _logger.Warning(LogCategory.Combat,$"[AITurnController] AI returned null action for unit {unit.Id}");
                }

                // Small delay between units
                yield return new WaitForSeconds(0.3f);
            }

            _logger.Info(LogCategory.Combat,$"[AITurnController] ProcessAITurn completed for {aiPlayer.Name}");
        }
    }
}
