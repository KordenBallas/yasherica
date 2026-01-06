using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Result of action execution.
    /// </summary>
    public class ActionResult
    {
        /// <summary>
        /// Whether the action was executed successfully.
        /// </summary>
        public bool Success { get; }
        
        /// <summary>
        /// The new game state after action execution.
        /// </summary>
        public ICombatState NewState { get; }
        
        /// <summary>
        /// Error message if execution failed.
        /// </summary>
        public string ErrorMessage { get; }
        
        private ActionResult(bool success, ICombatState newState, string errorMessage = null)
        {
            Success = success;
            NewState = newState;
            ErrorMessage = errorMessage;
        }
        
        public static ActionResult Successful(ICombatState newState)
        {
            return new ActionResult(true, newState);
        }
        
        public static ActionResult Failed(ICombatState currentState, string errorMessage)
        {
            return new ActionResult(false, currentState, errorMessage);
        }
    }
}

