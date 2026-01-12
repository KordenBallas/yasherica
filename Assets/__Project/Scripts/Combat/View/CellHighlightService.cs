using System.Collections.Generic;
using Combat.Battlefield;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Implementation of cell highlighting service.
    /// Now delegates to HexCellController for state management.
    /// No longer manages colors directly - states handle that.
    /// </summary>
    public class CellHighlightService : ICellHighlightService
    {
        private readonly HexCellController cellController;
        private readonly HashSet<HexCoordinates> highlightedCells = new();

        public CellHighlightService(HexCellController cellController)
        {
            this.cellController = cellController;
        }

        public void HighlightCell(HexCoordinates coords, HighlightType type)
        {
            cellController.HighlightCell(coords, type);
            highlightedCells.Add(coords);

            Debug.Log($"[CellHighlightService] Highlighting {coords} as {type}");
        }

        public void ClearHighlight()
        {
            // Clear all tracked highlights
            foreach (var coords in highlightedCells)
            {
                cellController.ClearHighlight(coords);
                Debug.Log($"[CellHighlightService] Clearing highlight for {coords}");
            }

            highlightedCells.Clear();
        }

        public void HighlightCells(IEnumerable<HexCoordinates> cells, HighlightType type)
        {
            foreach (var cell in cells)
            {
                HighlightCell(cell, type);
            }
        }
    }
}
