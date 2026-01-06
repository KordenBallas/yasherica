using Combat.Core;
using Combat.TurnManagement;
using System.Collections.Generic;
using UnityEngine;

namespace Combat.Controller
{
    /// <summary>
    /// Main controller that orchestrates the game loop.
    /// </summary>
    public interface ICombatController
    {
        /// <summary>
        /// Current game state.
        /// </summary>
        ICombatState CombatState { get; }
        
        /// <summary>
        /// Turn manager.
        /// </summary>
        ITurnManager TurnManager { get; }
        
        /// <summary>
        /// The battlefield instance.
        /// </summary>
        Battlefield.IBattlefield Battlefield { get; }
        
        /// <summary>
        /// Initializes the game with initial state and players.
        /// </summary>
        void Initialize(ICombatState initialState, System.Collections.Generic.IReadOnlyList<IPlayer> players);
        
        /// <summary>
        /// Processes a player action.
        /// </summary>
        Execution.ActionResult ProcessAction(IAction action);
        
        /// <summary>
        /// Updates the game logic (checks win conditions, etc.).
        /// </summary>
        void Update();
        
        /// <summary>
        /// Initializes the battlefield with geometric data from platform.
        /// </summary>
        void InitializeBattlefield(List<Vector3> boundary, Vector3 center);
        
        /// <summary>
        /// Cleans up battlefield when combat ends or platform is exited.
        /// </summary>
        void CleanupBattlefield();
        
        /// <summary>
        /// Event fired when the game state changes.
        /// </summary>
        event System.Action<ICombatState> OnStateChanged;
        
        /// <summary>
        /// Event fired when a player's turn starts.
        /// </summary>
        event System.Action<IPlayer> OnTurnStarted;
        
        /// <summary>
        /// Event fired when the game ends.
        /// </summary>
        event System.Action<IPlayer, CombatPhase> OnGameEnded;
    }
}

