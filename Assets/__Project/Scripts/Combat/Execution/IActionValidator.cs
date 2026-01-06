using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Validates actions before execution.
    /// </summary>
    public interface IActionValidator
    {
        /// <summary>
        /// Validates an action against the current game state.
        /// </summary>
        bool Validate(ICombatState gameState, IAction action);
        
        /// <summary>
        /// Validates an action with detailed result information.
        /// </summary>
        ValidationResult ValidateDetailed(ICombatState gameState, IAction action);
    }
}

