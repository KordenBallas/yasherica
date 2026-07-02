using Core.Logging;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Represents a highlighted cell with various highlight types.
    /// Encapsulates the different visual feedback states (hovered, selected, valid move, etc.).
    /// Stores the original color for restoration when clearing the highlight.
    /// </summary>
    public class HexCellHighlightedState : HexCellStateBase
    {
        private readonly HighlightType highlightType;
        private readonly Color highlightColor;
        private readonly Color originalColor;
        private readonly IGameLogger _logger;

        public override HexCellStateType StateType => HexCellStateType.Highlighted;

        /// <summary>
        /// Gets the specific highlight type of this state.
        /// </summary>
        public HighlightType HighlightType => highlightType;

        /// <summary>
        /// Creates a highlighted state.
        /// </summary>
        /// <param name="type">The type of highlight (Hovered, Selected, ValidMove, etc.)</param>
        /// <param name="color">The highlight color to display</param>
        /// <param name="originalColor">The color to restore when clearing the highlight</param>
        public HexCellHighlightedState(HighlightType type, Color color, Color originalColor, IGameLogger logger = null)
        {
            this.highlightType = type;
            this.highlightColor = color;
            this.originalColor = originalColor;
            _logger = logger;
        }

        public override void OnEnter(IHexCell cell)
        {
            cell.IsActive = true;
            _logger?.Info(LogCategory.Combat, $"[HexCellHighlightedState] Cell {cell.Coordinates} highlighted as {highlightType}");
        }

        public override Color GetColor()
        {
            return highlightColor;
        }

        /// <summary>
        /// Gets the original color before highlighting (for restoration).
        /// </summary>
        public Color GetOriginalColor()
        {
            return originalColor;
        }

        public override bool CanTransitionTo(IHexCellState targetState)
        {
            // Can transition to any state
            // Multiple highlights can override each other
            return true;
        }
    }
}
