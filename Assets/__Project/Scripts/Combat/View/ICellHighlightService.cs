using System.Collections.Generic;
using Combat.Battlefield;

namespace Combat.View
{
    /// <summary>
    /// Service for highlighting cells on the battlefield.
    /// Provides visual feedback for player interactions.
    /// </summary>
    public interface ICellHighlightService
    {
        /// <summary>
        /// Highlights a single cell with the specified type.
        /// </summary>
        void HighlightCell(HexCoordinates coords, HighlightType type);
        
        /// <summary>
        /// Clears all highlights.
        /// </summary>
        void ClearHighlight();
        
        /// <summary>
        /// Highlights multiple cells with the specified type.
        /// </summary>
        void HighlightCells(IEnumerable<HexCoordinates> cells, HighlightType type);
    }
    
    public enum HighlightType
    {
        Hovered,
        Selected,
        ValidMove,
        InvalidMove,
        EnemyThreat
    }
}
