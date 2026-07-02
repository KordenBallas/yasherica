using Core.Logging;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Default active state for a cell with no special status.
    /// This is the baseline "normal" appearance of a cell.
    /// </summary>
    public class HexCellIdleState : HexCellStateBase
    {
        private readonly Color idleColor;
        private readonly IGameLogger _logger;

        public override HexCellStateType StateType => HexCellStateType.Idle;

        /// <summary>
        /// Creates an idle state with an optional color.
        /// </summary>
        /// <param name="color">The idle color, defaults to white if not specified</param>
        public HexCellIdleState(Color? color = null, IGameLogger logger = null)
        {
            idleColor = color ?? Color.white;
            _logger = logger;
        }

        public override void OnEnter(IHexCell cell)
        {
            cell.IsActive = true;
            _logger?.Info(LogCategory.Combat, $"[HexCellIdleState] Cell {cell.Coordinates} is now idle");
        }

        public override Color GetColor()
        {
            return idleColor;
        }

        public override bool CanTransitionTo(IHexCellState targetState)
        {
            // Idle can transition to any other state
            return true;
        }
    }
}
