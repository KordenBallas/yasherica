using System.Collections;
using Combat.Controller;
using Combat.Core;
using UnityEngine;

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
            Debug.Log("[AITurnController] Initialized and subscribed to OnTurnStarted");
        }

        /// <summary>
        /// Cleanup - unsubscribe from events.
        /// </summary>
        public void Dispose()
        {
            if (_combatController != null)
            {
                _combatController.OnTurnStarted -= OnTurnStarted;
                Debug.Log("[AITurnController] Disposed and unsubscribed from OnTurnStarted");
            }
        }

        private void OnTurnStarted(IPlayer player)
        {
            Debug.Log($"[AITurnController] OnTurnStarted - Player: {player.Name} (Type: {player.Type})");

            if (player.Type == PlayerType.AI)
            {
                Debug.Log($"[AITurnController] AI player's turn - starting ProcessAITurn coroutine");
                if (_coroutineRunner != null)
                {
                    _coroutineRunner.StartCoroutine(ProcessAITurn(player));
                }
                else
                {
                    Debug.LogError("[AITurnController] Cannot process AI turn - no coroutine runner available!");
                }
            }
            else
            {
                Debug.Log($"[AITurnController] Human player's turn - no AI processing needed");
            }
        }

        private IEnumerator ProcessAITurn(IPlayer aiPlayer)
        {
            Debug.Log($"[AITurnController] ProcessAITurn started for {aiPlayer.Name}");

            var ai = aiPlayer as AIPlayer;
            if (ai == null)
            {
                Debug.LogError($"[AITurnController] Player {aiPlayer.Name} is not an AIPlayer!");
                yield break;
            }

            // Get all active units for this AI player
            var activeUnits = _combatController.CombatState.GetActiveUnitsByPlayer(aiPlayer);
            Debug.Log($"[AITurnController] AI has {activeUnits.Count} active unit(s)");

            foreach (var unit in activeUnits)
            {
                Debug.Log($"[AITurnController] Processing AI unit {unit.Id}");

                // Wait a bit for visual feedback
                yield return new WaitForSeconds(0.5f);

                // Request action from AI decision maker
                var action = ai.RequestAction(_combatController.CombatState, unit);

                if (action != null)
                {
                    Debug.Log($"[AITurnController] AI decided action: {action.Type} for unit {unit.Id}");

                    // Execute the action
                    var result = _combatController.ProcessAction(action);

                    if (result.Success)
                    {
                        Debug.Log($"[AITurnController] AI action executed successfully");
                    }
                    else
                    {
                        Debug.LogWarning($"[AITurnController] AI action failed: {result.ErrorMessage}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[AITurnController] AI returned null action for unit {unit.Id}");
                }

                // Small delay between units
                yield return new WaitForSeconds(0.3f);
            }

            Debug.Log($"[AITurnController] ProcessAITurn completed for {aiPlayer.Name}");
        }
    }
}
