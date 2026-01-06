using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Represents the target of an ability.
    /// Immutable struct that defines what the ability is targeting.
    /// </summary>
    public struct AbilityTarget
    {
        /// <summary>
        /// The type of target.
        /// </summary>
        public AbilityTargetType Type { get; }
        
        /// <summary>
        /// ID of the target unit (if Type is Enemy, Ally, or Self).
        /// </summary>
        public int? TargetUnitId { get; }
        
        /// <summary>
        /// Target position on the battlefield (if Type is Position or Area).
        /// </summary>
        public HexCoordinates? TargetPosition { get; }
        
        /// <summary>
        /// Creates an ability target for a unit.
        /// </summary>
        public static AbilityTarget ForUnit(int unitId, AbilityTargetType type)
        {
            return new AbilityTarget(type, unitId, null);
        }
        
        /// <summary>
        /// Creates an ability target for a position.
        /// </summary>
        public static AbilityTarget ForPosition(HexCoordinates position, AbilityTargetType type)
        {
            return new AbilityTarget(type, null, position);
        }
        
        /// <summary>
        /// Creates an ability target for self (no parameters needed).
        /// </summary>
        public static AbilityTarget ForSelf()
        {
            return new AbilityTarget(AbilityTargetType.Self, null, null);
        }
        
        private AbilityTarget(AbilityTargetType type, int? targetUnitId, HexCoordinates? targetPosition)
        {
            Type = type;
            TargetUnitId = targetUnitId;
            TargetPosition = targetPosition;
        }
    }
}

