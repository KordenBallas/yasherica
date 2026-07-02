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
        /// Adds a unit to the combat state.
        /// </summary>
        void AddUnit(IUnit unit);
        
        /// <summary>
        /// Processes a player action.
        /// </summary>
        Execution.ActionResult ProcessAction(IAction action);
        
        /// <summary>
        /// Updates the game logic (checks win conditions, etc.).
        /// </summary>
        void Update();
        
        /// <summary>
        /// Initializes the battlefield from the platform's hex surface (the combat grid is derived
        /// from the same cells the ground was built from — never re-fitted).
        /// </summary>
        void InitializeBattlefield(Combat.Battlefield.PlatformHexSurface surface, Vector3 center);
        
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

