using Combat.Core;
using Combat.Battlefield;
using System.Collections.Generic;
using System.Linq;

namespace Combat.Integration
{
    /// <summary>
    /// Adapter that wraps the existing IBattlefield for Combat system use.
    /// Provides combat-specific functionality on top of the battlefield.
    /// </summary>
    public class CombatBattlefield
    {
        private readonly IBattlefield _battlefield;
        
        public IBattlefield Battlefield => _battlefield;
        
        public CombatBattlefield(IBattlefield battlefield)
        {
            _battlefield = battlefield;
        }
        
        /// <summary>
        /// Gets all cells within a certain range from a position.
        /// </summary>
        public List<HexCoordinates> GetCellsInRange(HexCoordinates center, int range)
        {
            var cells = new List<HexCoordinates>();
            
            for (int q = -range; q <= range; q++)
            {
                for (int r = -range; r <= range; r++)
                {
                    var coord = new HexCoordinates(center.Q + q, center.R + r);
                    
                    if (CalculateDistance(center, coord) <= range && _battlefield.IsCellInBoundary(coord))
                    {
                        cells.Add(coord);
                    }
                }
            }
            
            return cells;
        }
        
        /// <summary>
        /// Gets valid movement positions for a unit within range.
        /// </summary>
        public List<HexCoordinates> GetValidMovementPositions(IUnit unit, int maxRange, ICombatState gameState)
        {
            var validPositions = new List<HexCoordinates>();
            var cellsInRange = GetCellsInRange(unit.Position, maxRange);
            
            foreach (var cell in cellsInRange)
            {
                // Skip current position
                if (cell.Equals(unit.Position))
                    continue;
                
                // Check if cell is occupied
                if (gameState.GetUnitAt(cell) != null)
                    continue;
                
                // Check if cell is walkable (if battlefield has this info)
                if (!_battlefield.IsCellInBoundary(cell))
                    continue;
                
                validPositions.Add(cell);
            }
            
            return validPositions;
        }
        
        /// <summary>
        /// Gets valid ability target positions within range.
        /// </summary>
        public List<HexCoordinates> GetValidAbilityTargetPositions(HexCoordinates casterPosition, int range)
        {
            return GetCellsInRange(casterPosition, range);
        }
        
        /// <summary>
        /// Calculates hex distance between two positions.
        /// </summary>
        public int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = System.Math.Abs(from.Q - to.Q);
            var dr = System.Math.Abs(from.R - to.R);
            var ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));
            
            return (dq + dr + ds) / 2;
        }
        
        /// <summary>
        /// Checks line of sight between two positions (for advanced features).
        /// </summary>
        public bool HasLineOfSight(HexCoordinates from, HexCoordinates to)
        {
            // Simple implementation - always true
            // Can be extended to check for obstacles
            return true;
        }
    }
}

