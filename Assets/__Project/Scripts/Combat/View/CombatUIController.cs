using Combat.Controller;
using Combat.Core;
using UnityEngine;
using TMPro;

namespace Combat.View
{
    /// <summary>
    /// Minimal UI controller for displaying combat information.
    /// Shows turn info, unit stats, and basic controls.
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _turnText;
        [SerializeField] private TextMeshProUGUI _currentPlayerText;
        [SerializeField] private TextMeshProUGUI _selectedUnitText;
        [SerializeField] private GameObject _unitInfoPanel;
        [SerializeField] private TextMeshProUGUI _gameStatusText;
        
        private ICombatController _gameController;
        private IUnit _selectedUnit;
        
        public void Initialize(ICombatController gameController)
        {
            _gameController = gameController;
            
            // Subscribe to events
            _gameController.OnStateChanged += OnStateChanged;
            _gameController.OnTurnStarted += OnTurnStarted;
            _gameController.OnGameEnded += OnGameEnded;
            
            // Initial update
            UpdateUI();
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
            UpdateUI();
        }
        
        private void OnTurnStarted(IPlayer player)
        {
            if (_gameStatusText != null)
            {
                _gameStatusText.text = $"{player.Name}'s turn started!";
            }
        }
        
        private void OnGameEnded(IPlayer winner, CombatPhase phase)
        {
            if (_gameStatusText != null)
            {
                if (winner != null)
                {
                    _gameStatusText.text = $"Game Over! {winner.Name} wins!";
                }
                else
                {
                    _gameStatusText.text = "Game Over!";
                }
            }
        }
        
        private void UpdateUI()
        {
            if (_gameController == null)
                return;
            
            var gameState = _gameController.CombatState;
            
            // Update turn info
            if (_turnText != null)
            {
                _turnText.text = $"Turn: {gameState.TurnNumber}";
            }
            
            // Update current player
            if (_currentPlayerText != null)
            {
                _currentPlayerText.text = $"Current Player: {gameState.CurrentPlayer.Name}";
            }
            
            // Update selected unit info
            if (_selectedUnit != null)
            {
                UpdateSelectedUnitInfo();
            }
            else
            {
                if (_unitInfoPanel != null)
                {
                    _unitInfoPanel.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Called when a unit is selected.
        /// </summary>
        public void OnUnitSelected(IUnit unit)
        {
            _selectedUnit = unit;
            UpdateSelectedUnitInfo();
        }
        
        /// <summary>
        /// Called when selection is cleared.
        /// </summary>
        public void ClearSelection()
        {
            _selectedUnit = null;
            if (_unitInfoPanel != null)
            {
                _unitInfoPanel.SetActive(false);
            }
        }
        
        private void UpdateSelectedUnitInfo()
        {
            if (_unitInfoPanel != null)
            {
                _unitInfoPanel.SetActive(_selectedUnit != null);
            }
            
            if (_selectedUnitText != null && _selectedUnit != null)
            {
                var text = $"Unit {_selectedUnit.Id}\n";
                text += $"HP: {_selectedUnit.CurrentHP}/{_selectedUnit.MaxHP}\n";
                text += $"State: {_selectedUnit.ActionState}\n";
                text += $"Abilities: {_selectedUnit.Abilities.Count}\n";
                text += $"Available: {_selectedUnit.GetAvailableAbilities().Count}\n";
                
                if (_selectedUnit.AbilityQueue.Count > 0)
                {
                    text += $"Queued: {_selectedUnit.AbilityQueue.Count}";
                }
                
                _selectedUnitText.text = text;
            }
        }
    }
}

