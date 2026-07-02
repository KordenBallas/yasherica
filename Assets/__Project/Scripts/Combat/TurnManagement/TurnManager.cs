using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using UnityEngine;

namespace Combat.TurnManagement
{
    /// <summary>
    /// Concrete implementation of turn manager using simple round-robin order.
    /// </summary>
    public class TurnManager : ITurnManager
    {
        private List<Core.IPlayer> _turnOrder;
        private int _currentPlayerIndex;
        private int _turnNumber;
        private readonly IGameLogger _logger;

        public Core.IPlayer CurrentPlayer => _turnOrder != null && _turnOrder.Count > 0 
            ? _turnOrder[_currentPlayerIndex] 
            : null;
        
        public int CurrentTurnNumber => _turnNumber;
        
        public IReadOnlyList<Core.IPlayer> TurnOrder => _turnOrder?.AsReadOnly();
        
        public TurnManager(IGameLogger logger)
        {
            _logger = logger;
            _currentPlayerIndex = 0;
            _turnNumber = 1;
        }
        
        public void Initialize(IReadOnlyList<Core.IPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                throw new System.ArgumentException("Cannot initialize TurnManager with null or empty player list.");
            }
            
            _turnOrder = players.ToList();
            _currentPlayerIndex = 0;
            _turnNumber = 1;
            _logger.Info(LogCategory.Combat,$"[TurnManager] Initialized with {players?.Count} players.");
        }
        
        public void NextTurn()
        {
            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                throw new System.InvalidOperationException("TurnManager not initialized. Call Initialize first.");
            }
            
            _currentPlayerIndex++;
            
            // If we've cycled through all players, increment turn number
            if (_currentPlayerIndex >= _turnOrder.Count)
            {
                _currentPlayerIndex = 0;
                _turnNumber++;
            }
        }
        
        public bool IsPlayerTurn(Core.IPlayer player)
        {
            if (player == null || CurrentPlayer == null)
                return false;
                
            return CurrentPlayer.Id == player.Id;
        }
    }
}

