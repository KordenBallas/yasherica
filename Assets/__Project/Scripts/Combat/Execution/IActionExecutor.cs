using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Executes actions and returns new game states.
    /// </summary>
    public interface IActionExecutor
    {
        /// <summary>
        /// Executes an action and returns the new game state.
        /// </summary>
        ICombatState Execute(ICombatState gameState, IAction action);
        
        /// <summary>
        /// Executes an action and returns detailed result.
        /// </summary>
        ActionResult ExecuteWithResult(ICombatState gameState, IAction action);
    }
}

