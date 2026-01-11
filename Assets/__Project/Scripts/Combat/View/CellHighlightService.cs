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
        private readonly Dictionary<HexCoordinates, Color> _originalColors = new();
        private readonly Dictionary<HexCoordinates, HighlightType> _currentHighlights = new();
        
        // Note: BattlefieldView integration will be added later
        // For now, we'll use debug logging
        
        public CellHighlightService(CombatMovementConfig config)
        {
            _config = config;
        }
        
        public void HighlightCell(HexCoordinates coords, HighlightType type)
        {
            Color color = GetColorForType(type);
            
            // Store current highlight
            _currentHighlights[coords] = type;
            
            // TODO: Integrate with BattlefieldView to actually change cell color
            // This requires BattlefieldView to expose cell modification methods
            Debug.Log($"[CellHighlightService] Highlighting {coords} as {type} with color {color}");
        }
        
        public void ClearHighlight()
        {
            // Restore original colors for all highlighted cells
            foreach (var coords in _currentHighlights.Keys)
            {
                // TODO: Restore cell color via BattlefieldView
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
