using Combat.Battlefield;
using Combat.Controller;
using Combat.Core;
using Combat.View;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Presenter for combat movement.
    /// Coordinates between input, validation, actions, and view updates.
    /// Follows MVP pattern - pure C# presenter logic.
    /// </summary>
    public class CombatMovementPresenter
    {
        private readonly ICombatController _combatController;
        private readonly IBattlefield _battlefield;
        private readonly ICellHighlightService _highlightService;
        private readonly IUnitPosition _playerUnit;
        
        private HexCoordinates? _hoveredCell;
        
        public CombatMovementPresenter(
            ICombatController combatController,
            IBattlefield battlefield,
            ICellHighlightService highlightService,
            IUnitPosition playerUnit)
        {
            _combatController = combatController;
            _battlefield = battlefield;
            _highlightService = highlightService;
            _playerUnit = playerUnit;
        }
        
        /// <summary>
        /// Updates cell highlighting based on target cell.
        /// </summary>
        public void UpdateHighlight(HexCoordinates? targetCell)
        {
            // Clear previous highlight
            if (_hoveredCell.HasValue)
                _highlightService.ClearHighlight();
            
            // Highlight new cell
            if (targetCell.HasValue)
            {
                bool isValid = IsValidMove(targetCell.Value);
                var highlightType = isValid ? HighlightType.Hovered : HighlightType.InvalidMove;
                _highlightService.HighlightCell(targetCell.Value, highlightType);
                _hoveredCell = targetCell;
            }
            else
            {
                _hoveredCell = null;
            }
        }
        
        /// <summary>
        /// Attempts to move the unit to the specified cell.
        /// </summary>
        public void TryMoveToCell(HexCoordinates targetCell)
        {
            Debug.Log($"[CombatMovementPresenter] TryMoveToCell called for {targetCell}");
            
            if (!IsValidMove(targetCell))
            {
                Debug.LogWarning($"[CombatMovementPresenter] Invalid move to {targetCell}");
                return;
            }
            
            Debug.Log($"[CombatMovementPresenter] Move validation passed for {targetCell}");
            
            // Create and submit move action
            var unit = _playerUnit as IUnit;
            
            if (unit == null)
            {
                Debug.LogError($"[CombatMovementPresenter] Failed to cast _playerUnit to IUnit (type: {_playerUnit?.GetType().Name ?? "null"})");
                return;
            }
            
            Debug.Log($"[CombatMovementPresenter] Creating MoveAction for unit {unit.Id} to {targetCell}");
            
            var moveAction = new MoveAction(unit.Owner, unit.Id, targetCell);
            var result = _combatController.ProcessAction(moveAction);
            
            if (!result.Success)
            {
                Debug.LogWarning($"[CombatMovementPresenter] Move failed: {result.ErrorMessage}");
            }
            else
            {
                Debug.Log($"[CombatMovementPresenter] Move successful to {targetCell}");
            }
        }
        
        private bool IsValidMove(HexCoordinates target)
        {
            // Check if cell is in battlefield boundary
            bool inBoundary = _battlefield.IsCellInBoundary(target);
            
            if (!inBoundary)
            {
                Debug.Log($"[CombatMovementPresenter] IsValidMove: {target} is OUT of boundary");
                return false;
            }
            
            // Check if adjacent to current position (distance == 1)
            int distance = CalculateDistance(_playerUnit.Position, target);
            bool isAdjacent = distance == 1;
            
            Debug.Log($"[CombatMovementPresenter] IsValidMove: {target} - InBoundary: {inBoundary}, Distance: {distance}, IsAdjacent: {isAdjacent}, CurrentPos: {_playerUnit.Position}");
            
            return isAdjacent;
        }
        
        private int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = Mathf.Abs(from.Q - to.Q);
            var dr = Mathf.Abs(from.R - to.R);
            var ds = Mathf.Abs((from.Q + from.R) - (to.Q + to.R));
            return (dq + dr + ds) / 2;
        }
    }
}
