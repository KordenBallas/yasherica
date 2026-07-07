using Combat.Core;
using Combat.Battlefield;
using System;

namespace Combat.Rules
{
    /// <summary>
    /// Rules for unit movement on the battlefield.
    /// </summary>
    public class MovementRules
    {
        /// <summary>
        /// Maximum movement range for units (in hex cells).
        /// </summary>
        public int MaxMovementRange { get; set; } = 3;
        
        /// <summary>
        /// Checks if a unit can move to a target position.
        /// </summary>
        public bool CanMoveTo(IUnit unit, HexCoordinates targetPosition, ICombatState gameState)
        {
            if (!unit.CanMove())
                return false;
            
            // Check if position is occupied
            if (gameState.GetUnitAt(targetPosition) != null)
                return false;
            
            // Check distance against the status-gated effective range (root/slow)
            int distance = CalculateDistance(unit.Position, targetPosition);
            return distance <= MovementRange.EffectiveFor(unit, MaxMovementRange);
        }
        
        /// <summary>
        /// Calculates hex distance between two positions.
        /// </summary>
        public int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = Math.Abs(from.Q - to.Q);
            var dr = Math.Abs(from.R - to.R);
            var ds = Math.Abs((from.Q + from.R) - (to.Q + to.R));
            
            return (dq + dr + ds) / 2;
        }
    }
}

