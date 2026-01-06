using Combat.Core;
using Combat.Controller;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Combat.View
{
    /// <summary>
    /// Combat log that displays a text feed of game events.
    /// Shows actions, damage, healing, and status effects.
    /// </summary>
    public class CombatLog : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private int _maxLogEntries = 20;
        
        private List<string> _logEntries = new List<string>();
        private ICombatController _gameController;
        
        public void Initialize(ICombatController gameController)
        {
            _gameController = gameController;
            
            _gameController.OnStateChanged += OnStateChanged;
            _gameController.OnTurnStarted += OnTurnStarted;
            _gameController.OnGameEnded += OnGameEnded;
        }
        
        private void OnDestroy()
        {
            if (_gameController != null)
            {
                _gameController.OnStateChanged -= OnStateChanged;
                _gameController.OnTurnStarted -= OnTurnStarted;
                _gameController.OnGameEnded -= OnGameEnded;
            }
        }
        
        private void OnStateChanged(ICombatState newState)
        {
            // Log can be expanded to track state changes
        }
        
        private void OnTurnStarted(IPlayer player)
        {
            AddLogEntry($"<color=yellow>Turn {_gameController.CombatState.TurnNumber}: {player.Name}'s turn</color>");
        }
        
        private void OnGameEnded(IPlayer winner, CombatPhase phase)
        {
            if (winner != null)
            {
                AddLogEntry($"<color=green><b>GAME OVER! {winner.Name} wins!</b></color>");
            }
            else
            {
                AddLogEntry($"<color=red><b>GAME OVER!</b></color>");
            }
        }
        
        /// <summary>
        /// Logs a unit action.
        /// </summary>
        public void LogAction(IAction action)
        {
            string message = action.Type switch
            {
                ActionType.Move => $"Unit {action.UnitId} moved",
                ActionType.ScheduleAbility => $"Unit {action.UnitId} scheduled an ability",
                ActionType.ExecuteAbilityQueue => $"Unit {action.UnitId} executed abilities",
                ActionType.EndUnitTurn => $"Unit {action.UnitId} ended turn",
                _ => $"Unit {action.UnitId} performed {action.Type}"
            };
            
            AddLogEntry(message);
        }
        
        /// <summary>
        /// Logs damage dealt.
        /// </summary>
        public void LogDamage(int attackerId, int targetId, int damage)
        {
            AddLogEntry($"<color=red>Unit {attackerId} dealt {damage} damage to Unit {targetId}</color>");
        }
        
        /// <summary>
        /// Logs healing.
        /// </summary>
        public void LogHealing(int healerId, int targetId, int amount)
        {
            AddLogEntry($"<color=green>Unit {healerId} healed Unit {targetId} for {amount} HP</color>");
        }
        
        /// <summary>
        /// Logs a status effect being applied.
        /// </summary>
        public void LogStatusEffect(int targetId, IStatusEffect effect)
        {
            AddLogEntry($"<color=orange>Unit {targetId} affected by {effect.Name}</color>");
        }
        
        /// <summary>
        /// Logs a unit death.
        /// </summary>
        public void LogUnitDeath(int unitId, string playerName)
        {
            AddLogEntry($"<color=red><b>Unit {unitId} ({playerName}) was defeated!</b></color>");
        }
        
        private void AddLogEntry(string entry)
        {
            _logEntries.Add($"[{System.DateTime.Now:HH:mm:ss}] {entry}");
            
            // Keep only the last N entries
            if (_logEntries.Count > _maxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }
            
            UpdateLogDisplay();
        }
        
        private void UpdateLogDisplay()
        {
            if (_logText != null)
            {
                _logText.text = string.Join("\n", _logEntries);
            }
        }
        
        /// <summary>
        /// Clears the combat log.
        /// </summary>
        public void ClearLog()
        {
            _logEntries.Clear();
            UpdateLogDisplay();
        }
    }
}

