namespace Combat.Core
{
    /// <summary>
    /// Represents unit action state and turn information.
    /// Part of interface segregation for IUnit.
    /// </summary>
    public interface IUnitActionState
    {
        /// <summary>
        /// Indicates whether this unit has acted this turn.
        /// </summary>
        bool HasActedThisTurn { get; }
        
        /// <summary>
        /// Indicates whether this unit can perform actions.
        /// </summary>
        bool CanAct { get; }
        
        /// <summary>
        /// Current action state of the unit.
        /// </summary>
        UnitActionState ActionState { get; }
        
        /// <summary>
        /// Indicates whether this unit can move.
        /// </summary>
        bool CanMove();
    }
}
