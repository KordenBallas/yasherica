namespace Combat.Core
{
    /// <summary>
    /// Base interface for all player actions.
    /// Represents an immutable command that can be validated and executed.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// The player performing this action.
        /// </summary>
        IPlayer Player { get; }
        
        /// <summary>
        /// ID of the unit performing this action.
        /// </summary>
        int UnitId { get; }
        
        /// <summary>
        /// The type of action.
        /// </summary>
        ActionType Type { get; }
        
        /// <summary>
        /// Indicates whether this action ends the unit's turn.
        /// </summary>
        bool EndsTurn { get; }
    }
}

