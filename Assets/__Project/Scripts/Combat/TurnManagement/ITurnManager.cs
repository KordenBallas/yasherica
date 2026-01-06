using System.Collections.Generic;

namespace Combat.TurnManagement
{
    /// <summary>
    /// Manages turn order and player switching.
    /// </summary>
    public interface ITurnManager
    {
        /// <summary>
        /// The player whose turn it currently is.
        /// </summary>
        Core.IPlayer CurrentPlayer { get; }
        
        /// <summary>
        /// Current turn number (increments with each full round).
        /// </summary>
        int CurrentTurnNumber { get; }
        
        /// <summary>
        /// The order in which players take turns.
        /// </summary>
        IReadOnlyList<Core.IPlayer> TurnOrder { get; }
        
        /// <summary>
        /// Initializes the turn manager with players.
        /// </summary>
        void Initialize(IReadOnlyList<Core.IPlayer> players);
        
        /// <summary>
        /// Advances to the next player's turn.
        /// </summary>
        void NextTurn();
        
        /// <summary>
        /// Checks if it's the specified player's turn.
        /// </summary>
        bool IsPlayerTurn(Core.IPlayer player);
    }
}

