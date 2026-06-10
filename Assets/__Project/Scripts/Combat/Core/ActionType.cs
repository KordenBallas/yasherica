namespace Combat.Core
{
    /// <summary>
    /// Defines the type of action a player can take.
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Move a unit to a different position.
        /// </summary>
        Move,
        
        /// <summary>
        /// Schedule an ability to be executed later.
        /// </summary>
        ScheduleAbility,
        
        /// <summary>
        /// Execute all scheduled abilities in the queue.
        /// </summary>
        ExecuteAbilityQueue,
        
        /// <summary>
        /// Reorder abilities in the execution queue.
        /// </summary>
        ReorderAbilities,
        
        /// <summary>
        /// Change the target of a scheduled ability.
        /// </summary>
        RetargetAbility,
        
        /// <summary>
        /// Explicitly end the unit's turn without taking an action.
        /// </summary>
        EndUnitTurn,

        /// <summary>
        /// Change the direction a unit is facing (free action by default).
        /// </summary>
        ChangeDirection
    }
}

