using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Controller;
using Combat.Core;
using Core.Logging;
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
        private readonly HexCellController _cellController;
        private readonly IUnitPosition _playerUnit;
        private readonly IGameLogger _logger;

        private HexCoordinates? _hoveredCell;
        private readonly HashSet<HexCoordinates> _highlightedCells = new();

        public CombatMovementPresenter(
            ICombatController combatController,
            IBattlefield battlefield,
            HexCellController cellController,
            IUnitPosition playerUnit,
            IGameLogger logger)
        {
            _combatController = combatController;
            _battlefield = battlefield;
            _cellController = cellController;
            _playerUnit = playerUnit;
            _logger = logger;
        }
        
        /// <summary>
        /// Updates cell highlighting based on target cell.
        /// </summary>
        public void UpdateHighlight(HexCoordinates? targetCell)
        {
            // Clear all previous highlights
            ClearAllHighlights();

            // Highlight new cell
            if (targetCell.HasValue)
            {
                bool isValid = IsValidMove(targetCell.Value);
                var highlightType = isValid ? HighlightType.Hovered : HighlightType.InvalidMove;
                _cellController.HighlightCell(targetCell.Value, highlightType);
                _highlightedCells.Add(targetCell.Value);
                _hoveredCell = targetCell;
            }
            else
            {
                _hoveredCell = null;
            }
        }

        /// <summary>
        /// Clears all highlighted cells.
        /// </summary>
        private void ClearAllHighlights()
        {
            foreach (var coords in _highlightedCells)
            {
                _cellController.ClearHighlight(coords);
            }
            _highlightedCells.Clear();
        }
        
        /// <summary>
        /// Attempts to move the unit to the specified cell.
        /// </summary>
        public void TryMoveToCell(HexCoordinates targetCell)
        {
            _logger.Info(LogCategory.Combat,$"[CombatMovementPresenter] TryMoveToCell called for {targetCell}");
            
            if (!IsValidMove(targetCell))
            {
                _logger.Warning(LogCategory.Combat,$"[CombatMovementPresenter] Invalid move to {targetCell}");
                return;
            }
            
            _logger.Info(LogCategory.Combat,$"[CombatMovementPresenter] Move validation passed for {targetCell}");
            
            // Create and submit move action
            var unit = _playerUnit as IUnit;
            
            if (unit == null)
            {
                _logger.Error(LogCategory.Combat,$"[CombatMovementPresenter] Failed to cast _playerUnit to IUnit (type: {_playerUnit?.GetType().Name ?? "null"})");
                return;
            }
            
            _logger.Info(LogCategory.Combat,$"[CombatMovementPresenter] Creating MoveAction for unit {unit.Id} to {targetCell}");
            
            var moveAction = new MoveAction(unit.Owner, unit.Id, targetCell);
            var result = _combatController.ProcessAction(moveAction);
            
            if (!result.Success)
            {
                _logger.Warning(LogCategory.Combat,$"[CombatMovementPresenter] Move failed: {result.ErrorMessage}");
            }
            else
            {
                _logger.Info(LogCategory.Combat,$"[CombatMovementPresenter] Move successful to {targetCell}");
            }
        }
        
        private bool IsValidMove(HexCoordinates target)
        {
            // Check if cell is in battlefield boundary
            bool inBoundary = _battlefield.IsCellInBoundary(target);
            
            if (!inBoundary)
            {
                _logger.Info(LogCategory.Combat,$"[CombatMovementPresenter] IsValidMove: {target} is OUT of boundary");
                return false;
            }
            
            // Check if adjacent to current position (distance == 1)
            int distance = CalculateDistance(_playerUnit.Position, target);
            bool isAdjacent = distance == 1;
            
            _logger.Info(LogCategory.Combat,$"[CombatMovementPresenter] IsValidMove: {target} - InBoundary: {inBoundary}, Distance: {distance}, IsAdjacent: {isAdjacent}, CurrentPos: {_playerUnit.Position}");
            
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
