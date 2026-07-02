using Core.Logging;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Represents a cell that is visible but cannot be interacted with.
    /// Used for obstacles, blocked areas, or temporarily disabled cells.
    /// </summary>
    public class HexCellDisabledState : HexCellStateBase
    {
        private readonly Color disabledColor;
        private readonly string disabledReason;
        private readonly IGameLogger _logger;

        public override HexCellStateType StateType => HexCellStateType.Disabled;

        /// <summary>
        /// Gets the reason why this cell is disabled (for debugging).
        /// </summary>
        public string DisabledReason => disabledReason;

        /// <summary>
        /// Creates a disabled state.
        /// </summary>
        /// <param name="reason">Optional reason for disabling (for debugging)</param>
        /// <param name="color">Optional color override, defaults to dark gray</param>
        public HexCellDisabledState(string reason = "Disabled", Color? color = null, IGameLogger logger = null)
        {
            disabledReason = reason;
            disabledColor = color ?? new Color(0.3f, 0.3f, 0.3f, 0.5f); // Dark gray
            _logger = logger;
        }

        public override void OnEnter(IHexCell cell)
        {
            cell.IsActive = true;
            _logger?.Info(LogCategory.Combat, $"[HexCellDisabledState] Cell {cell.Coordinates} disabled: {disabledReason}");
        }

        public override Color GetColor()
        {
            return disabledColor;
        }

        public override bool CanTransitionTo(IHexCellState targetState)
        {
            // Disabled cells can only transition back to Idle or Inactive
            // Cannot be highlighted or occupied while disabled
            return targetState.StateType == HexCellStateType.Idle ||
                   targetState.StateType == HexCellStateType.Inactive;
        }
    }
}
