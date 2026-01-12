using Combat.Config;
using Combat.Core;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Controller/Presenter for HexCell state management.
    /// Follows MVP pattern - pure C# logic coordinating between services and cells.
    /// Manages state transitions, highlighting, occupation, and disabling of cells.
    /// </summary>
    public class HexCellController
    {
        private readonly IBattlefield battlefield;
        private readonly CombatMovementConfig config;

        public HexCellController(IBattlefield battlefield, CombatMovementConfig config)
        {
            this.battlefield = battlefield;
            this.config = config;
        }

        /// <summary>
        /// Highlights a cell with the specified type.
        /// Creates appropriate state and applies it.
        /// </summary>
        /// <param name="coords">The coordinates of the cell to highlight</param>
        /// <param name="type">The type of highlight to apply</param>
        public void HighlightCell(HexCoordinates coords, HighlightType type)
        {
            var cell = battlefield.GetCellAt(coords);
            if (cell == null)
            {
                Debug.LogWarning($"[HexCellController] Cannot highlight {coords} - cell not found");
                return;
            }

            // Get original color (from current state or default)
            Color originalColor = cell.StateMachine.CurrentState?.GetColor() ?? Color.white;

            // Get highlight color based on type
            Color highlightColor = GetColorForHighlightType(type);

            // Create and apply highlighted state
            var highlightState = new HexCellHighlightedState(type, highlightColor, originalColor);
            cell.ChangeState(highlightState);
        }

        /// <summary>
        /// Clears highlight and returns cell to appropriate base state.
        /// </summary>
        /// <param name="coords">The coordinates of the cell to clear</param>
        public void ClearHighlight(HexCoordinates coords)
        {
            var cell = battlefield.GetCellAt(coords);
            if (cell == null) return;

            // Determine appropriate state to return to
            if (cell.StateMachine.CurrentState is HexCellHighlightedState highlightState)
            {
                // Return to original color
                Color originalColor = highlightState.GetOriginalColor();
                var idleState = new HexCellIdleState(originalColor);
                cell.ChangeState(idleState);
            }
        }

        /// <summary>
        /// Marks a cell as occupied by a unit.
        /// </summary>
        /// <param name="coords">The coordinates of the cell</param>
        /// <param name="unit">The unit occupying the cell</param>
        public void SetCellOccupied(HexCoordinates coords, IUnit unit)
        {
            var cell = battlefield.GetCellAt(coords);
            if (cell == null)
            {
                Debug.LogWarning($"[HexCellController] Cannot occupy {coords} - cell not found");
                return;
            }

            var occupiedState = new HexCellOccupiedState(unit);
            cell.ChangeState(occupiedState);
        }

        /// <summary>
        /// Clears occupation and returns cell to idle state.
        /// </summary>
        /// <param name="coords">The coordinates of the cell</param>
        public void ClearCellOccupation(HexCoordinates coords)
        {
            var cell = battlefield.GetCellAt(coords);
            if (cell == null) return;

            if (cell.StateMachine.IsInState(HexCellStateType.Occupied))
            {
                var idleState = new HexCellIdleState();
                cell.ChangeState(idleState);
            }
        }

        /// <summary>
        /// Disables a cell with a reason.
        /// Disabled cells are visible but cannot be interacted with.
        /// </summary>
        /// <param name="coords">The coordinates of the cell</param>
        /// <param name="reason">Optional reason for disabling (for debugging)</param>
        public void DisableCell(HexCoordinates coords, string reason = "Disabled")
        {
            var cell = battlefield.GetCellAt(coords);
            if (cell == null) return;

            var disabledState = new HexCellDisabledState(reason);
            cell.ChangeState(disabledState);
        }

        /// <summary>
        /// Enables a previously disabled cell.
        /// </summary>
        /// <param name="coords">The coordinates of the cell</param>
        public void EnableCell(HexCoordinates coords)
        {
            var cell = battlefield.GetCellAt(coords);
            if (cell == null) return;

            if (cell.StateMachine.IsInState(HexCellStateType.Disabled))
            {
                var idleState = new HexCellIdleState();
                cell.ChangeState(idleState);
            }
        }

        /// <summary>
        /// Queries if a cell is in a specific state.
        /// </summary>
        /// <param name="coords">The coordinates of the cell</param>
        /// <param name="stateType">The state type to check for</param>
        /// <returns>True if the cell is in the specified state</returns>
        public bool IsCellInState(HexCoordinates coords, HexCellStateType stateType)
        {
            var cell = battlefield.GetCellAt(coords);
            return cell?.StateMachine.IsInState(stateType) ?? false;
        }

        /// <summary>
        /// Gets the highlight type of a cell if it's highlighted.
        /// </summary>
        /// <param name="coords">The coordinates of the cell</param>
        /// <returns>The highlight type if highlighted, null otherwise</returns>
        public HighlightType? GetCellHighlightType(HexCoordinates coords)
        {
            var cell = battlefield.GetCellAt(coords);
            if (cell?.StateMachine.CurrentState is HexCellHighlightedState highlightState)
            {
                return highlightState.HighlightType;
            }
            return null;
        }

        /// <summary>
        /// Maps a HighlightType to its corresponding color from config.
        /// </summary>
        private Color GetColorForHighlightType(HighlightType type)
        {
            return type switch
            {
                HighlightType.Hovered => config.hoveredCellColor,
                HighlightType.ValidMove => config.validMoveCellColor,
                HighlightType.InvalidMove => config.invalidMoveCellColor,
                HighlightType.Selected => config.selectedCellColor,
                HighlightType.EnemyThreat => new Color(1f, 0.5f, 0f, 0.4f), // Orange
                _ => Color.white
            };
        }
    }
}
