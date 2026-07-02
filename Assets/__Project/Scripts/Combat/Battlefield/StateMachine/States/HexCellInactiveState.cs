using Core.Logging;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Represents a cell that exists but is not visible or active.
    /// Typically used before battlefield initialization or after deactivation.
    /// </summary>
    public class HexCellInactiveState : HexCellStateBase
    {
        private readonly IGameLogger _logger;

        public override HexCellStateType StateType => HexCellStateType.Inactive;

        public HexCellInactiveState(IGameLogger logger = null)
        {
            _logger = logger;
        }

        public override void OnEnter(IHexCell cell)
        {
            cell.IsActive = false;
            _logger?.Info(LogCategory.Combat, $"[HexCellInactiveState] Cell {cell.Coordinates} is now inactive");
        }

        public override Color GetColor()
        {
            return Color.clear; // Transparent/invisible
        }

        public override bool CanTransitionTo(IHexCellState targetState)
        {
            // Inactive can transition to any other state
            return targetState?.StateType != HexCellStateType.Inactive;
        }
    }
}
