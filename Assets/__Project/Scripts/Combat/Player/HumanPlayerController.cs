using Combat.Controller;
using Combat.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Combat.Battlefield;
using System.Collections.Generic;

namespace Combat.Player
{
    /// <summary>
    /// MonoBehaviour controller for human player input.
    /// Thin adapter following MVP pattern - delegates logic to HumanPlayer.
    /// </summary>
    public class HumanPlayerController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        
        private ICombatController _gameController;
        private IPlayer _player;
        private IUnit _selectedUnit;
        
        /// <summary>
        /// Event fired when a unit is selected.
        /// </summary>
        public event System.Action<IUnit> OnUnitSelected;
        
        /// <summary>
        /// Event fired when an action is requested.
        /// </summary>
        public event System.Action<IAction> OnActionRequested;
        
        public void Initialize(IPlayer player, ICombatController gameController)
        {
            _player = player;
            _gameController = gameController;
            
            if (_camera == null)
                _camera = Camera.main;
        }
        
        private void Update()
        {
            // Only process input if it's this player's turn
            if (_gameController == null || _gameController.TurnManager.CurrentPlayer.Id != _player.Id)
                return;
            
            // Handle mouse click for unit selection
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseClick();
            }
            
            // Handle keyboard input for selected unit
            if (_selectedUnit != null)
            {
                HandleUnitInput();
            }
        }
        
        private void HandleMouseClick()
        {
            Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Try to find unit at clicked position
                // This is a placeholder - actual implementation would use hex grid raycasting
                var unitView = hit.collider.GetComponent<View.UnitView>();
                if (unitView != null)
                {
                    SelectUnit(unitView.Unit);
                }
            }
        }
        
        private void SelectUnit(IUnit unit)
        {
            // Only select units owned by this player
            if (unit.Owner.Id != _player.Id)
                return;
            
            // Only select units that can still act
            if (!unit.CanAct || unit.HasActedThisTurn)
                return;
            
            _selectedUnit = unit;
            OnUnitSelected?.Invoke(_selectedUnit);
            
            Debug.Log($"Selected unit {_selectedUnit.Id}");
        }
        
        private void HandleUnitInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            
            // End unit turn with Space
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                RequestEndUnitTurn();
            }
        }
        
        /// <summary>
        /// Requests a move action for the selected unit.
        /// Called by UI or other systems.
        /// </summary>
        public void RequestMoveAction(HexCoordinates targetPosition)
        {
            if (_selectedUnit == null)
                return;
            
            var action = new MoveAction(_player, _selectedUnit.Id, targetPosition);
            OnActionRequested?.Invoke(action);
            
            var result = _gameController.ProcessAction(action);
            
            if (result.Success)
            {
                Debug.Log($"Unit {_selectedUnit.Id} moved to {targetPosition.Q},{targetPosition.R}");
                _selectedUnit = null; // Deselect after action
            }
            else
            {
                Debug.LogWarning($"Move failed: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Requests scheduling an ability.
        /// </summary>
        public void RequestScheduleAbility(int abilityId, AbilityTarget target)
        {
            if (_selectedUnit == null)
                return;
            
            var action = new ScheduleAbilityAction(_player, _selectedUnit.Id, abilityId, target);
            OnActionRequested?.Invoke(action);
            
            var result = _gameController.ProcessAction(action);
            
            if (result.Success)
            {
                Debug.Log($"Ability {abilityId} scheduled for unit {_selectedUnit.Id}");
            }
            else
            {
                Debug.LogWarning($"Schedule ability failed: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Requests executing the ability queue.
        /// </summary>
        public void RequestExecuteAbilityQueue()
        {
            if (_selectedUnit == null)
                return;
            
            var action = new ExecuteAbilityQueueAction(_player, _selectedUnit.Id);
            OnActionRequested?.Invoke(action);
            
            var result = _gameController.ProcessAction(action);
            
            if (result.Success)
            {
                Debug.Log($"Ability queue executed for unit {_selectedUnit.Id}");
                _selectedUnit = null; // Deselect after action
            }
            else
            {
                Debug.LogWarning($"Execute queue failed: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Requests ending the unit's turn without action.
        /// </summary>
        public void RequestEndUnitTurn()
        {
            if (_selectedUnit == null)
                return;
            
            var action = new EndUnitTurnAction(_player, _selectedUnit.Id);
            OnActionRequested?.Invoke(action);
            
            var result = _gameController.ProcessAction(action);
            
            if (result.Success)
            {
                Debug.Log($"Unit {_selectedUnit.Id} turn ended");
                _selectedUnit = null; // Deselect after action
            }
            else
            {
                Debug.LogWarning($"End turn failed: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Gets valid move positions for the selected unit.
        /// </summary>
        public List<HexCoordinates> GetValidMovePositions()
        {
            if (_selectedUnit == null)
                return new List<HexCoordinates>();
            
            // This is a placeholder - actual implementation would use MovementRules
            // and battlefield to calculate valid positions
            var validPositions = new List<HexCoordinates>();
            
            // For now, return positions within range
            for (int q = -3; q <= 3; q++)
            {
                for (int r = -3; r <= 3; r++)
                {
                    var pos = new HexCoordinates(_selectedUnit.Position.Q + q, _selectedUnit.Position.R + r);
                    validPositions.Add(pos);
                }
            }
            
            return validPositions;
        }
    }
}

