namespace Combat.Core
{
    /// <summary>
    /// Represents a condition that, when met, ends the game.
    /// </summary>
    public interface IWinCondition
    {
        /// <summary>
        /// Type of win condition.
        /// </summary>
        WinConditionType Type { get; }
        
        /// <summary>
        /// Checks if the win condition is met for the given game state.
        /// </summary>
        /// <param name="gameState">Current game state to evaluate.</param>
        /// <param name="winningPlayer">The player who won, if condition is met.</param>
        /// <returns>True if the win condition is met.</returns>
        bool Check(ICombatState gameState, out IPlayer winningPlayer);
    }
}

