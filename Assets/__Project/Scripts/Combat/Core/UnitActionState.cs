namespace Combat.Core
{
    /// <summary>
    /// Represents the action state of a unit during combat.
    /// </summary>
    public enum UnitActionState
    {
        /// <summary>
        /// Unit is ready to act this turn.
        /// </summary>
        Ready,
        
        /// <summary>
        /// Unit has already acted this turn.
        /// </summary>
        ActedThisTurn,
        
        /// <summary>
        /// Unit is stunned and cannot act.
        /// </summary>
        Stunned,
        
        /// <summary>
        /// Unit is dead.
        /// </summary>
        Dead
    }
}

