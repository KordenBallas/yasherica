using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using UnityEngine;

namespace Combat.TurnManagement
{
    /// <summary>
    /// Turn bookkeeping for the phase round: enemies no longer take turns (they plan and
    /// resolve as phases), so the current player is pinned to the human and NextTurn simply
    /// advances the round counter. IsPlayerTurn keeps meaning "the human may act".
    /// </summary>
    public class TurnManager : ITurnManager
    {
        private List<Core.IPlayer> _turnOrder;
        private Core.IPlayer _humanPlayer;
        private int _turnNumber;
        private readonly IGameLogger _logger;

        public Core.IPlayer CurrentPlayer => _humanPlayer;

        public int CurrentTurnNumber => _turnNumber;

        public IReadOnlyList<Core.IPlayer> TurnOrder => _turnOrder?.AsReadOnly();

        public TurnManager(IGameLogger logger)
        {
            _logger = logger;
            _turnNumber = 1;
        }

        public void Initialize(IReadOnlyList<Core.IPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                throw new System.ArgumentException("Cannot initialize TurnManager with null or empty player list.");
            }

            _turnOrder = players.ToList();
            _humanPlayer = _turnOrder.FirstOrDefault(p => p.Type == Core.PlayerType.Human) ?? _turnOrder[0];
            _turnNumber = 1;
            _logger.Info(LogCategory.Combat,$"[TurnManager] Initialized with {players?.Count} players; acting player is {_humanPlayer.Name}.");
        }

        public void NextTurn()
        {
            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                throw new System.InvalidOperationException("TurnManager not initialized. Call Initialize first.");
            }

            _turnNumber++;
        }
        
        public bool IsPlayerTurn(Core.IPlayer player)
        {
            if (player == null || CurrentPlayer == null)
                return false;
                
            return CurrentPlayer.Id == player.Id;
        }
    }
}

