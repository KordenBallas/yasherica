using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Implementation of cell highlighting service.
    /// Manages visual feedback for cell interactions.
    /// </summary>
    public class CellHighlightService : ICellHighlightService
    {
        private readonly CombatMovementConfig _config;
        private readonly IBattlefield _battlefield;
        private readonly Dictionary<HexCoordinates, Color> _originalColors = new();
        private readonly Dictionary<HexCoordinates, HighlightType> _currentHighlights = new();

        public CellHighlightService(CombatMovementConfig config, IBattlefield battlefield)
        {
            _config = config;
            _battlefield = battlefield;
        }
        
        public void HighlightCell(HexCoordinates coords, HighlightType type)
        {
            Color color = GetColorForType(type);

            // Get the cell from battlefield
            var cell = _battlefield.GetCellAt(coords);
            if (cell == null)
            {
                Debug.LogWarning($"[CellHighlightService] Cannot highlight {coords} - cell not found");
                return;
            }

            // Save original color (only on first highlight)
            if (!_originalColors.ContainsKey(coords))
            {
                _originalColors[coords] = cell.Color;
            }

            // Apply highlight color
            cell.Color = color;

            // Track current highlight type
            _currentHighlights[coords] = type;

            Debug.Log($"[CellHighlightService] Highlighting {coords} as {type} with color {color}");
        }
        
        public void ClearHighlight()
        {
            // Restore original colors for all highlighted cells
            foreach (var kvp in _originalColors)
            {
                var coords = kvp.Key;
                var originalColor = kvp.Value;

                var cell = _battlefield.GetCellAt(coords);
                if (cell == null)
                {
                    Debug.LogWarning($"[CellHighlightService] Cannot clear highlight for {coords} - cell not found");
                    continue;
                }

                cell.Color = originalColor;
                Debug.Log($"[CellHighlightService] Clearing highlight for {coords}");
            }

            _originalColors.Clear();
            _currentHighlights.Clear();
        }
        
        public void HighlightCells(IEnumerable<HexCoordinates> cells, HighlightType type)
        {
            foreach (var cell in cells)
            {
                HighlightCell(cell, type);
            }
        }
        
        private Color GetColorForType(HighlightType type)
        {
            return type switch
            {
                HighlightType.Hovered => _config.hoveredCellColor,
                HighlightType.ValidMove => _config.validMoveCellColor,
                HighlightType.InvalidMove => _config.invalidMoveCellColor,
                HighlightType.Selected => _config.selectedCellColor,
                HighlightType.EnemyThreat => new Color(1f, 0.5f, 0f, 0.4f), // Orange
                _ => Color.white
            };
        }
    }
}
