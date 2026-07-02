using Combat.Core;
using Core.Logging;
using System.Linq;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Win condition: Survive for a specified number of turns.
    /// If any player survives the turn limit, they win.
    /// </summary>
    public class SurviveTurnsWinCondition : IWinCondition
    {
        public WinConditionType Type => WinConditionType.SurviveTurns;
        
        private readonly int _targetTurns;
        private readonly int _targetPlayerId;
        private readonly IGameLogger _logger;

        public SurviveTurnsWinCondition(int targetTurns, int targetPlayerId, IGameLogger logger = null)
        {
            _targetTurns = targetTurns;
            _targetPlayerId = targetPlayerId;
            _logger = logger;
        }
        
        public bool Check(ICombatState gameState, out IPlayer winningPlayer)
        {
            if (gameState.TurnNumber >= _targetTurns)
            {
                // Find the target player
                winningPlayer = gameState.Players.FirstOrDefault(p => p.Id == _targetPlayerId);
                _logger?.Info(LogCategory.Combat,$"[SurviveTurnsWinCondition] Target player check {winningPlayer?.Id} {winningPlayer?.Name}.");
                
                // Check if they have any alive units
                if (winningPlayer != null)
                {
                    var aliveUnits = gameState.GetUnitsByPlayer(winningPlayer).Where(u => u.IsAlive);
                    if (aliveUnits.Any())
                    {
                        _logger?.Info(LogCategory.Combat,$"[SurviveTurnsWinCondition] Survive turns win for player {winningPlayer.Id} {winningPlayer.Name}");
                        return true;
                    }
                }
            }
            
            _logger?.Info(LogCategory.Combat,$"[SurviveTurnsWinCondition] Survive turns not met. Turn number {gameState.TurnNumber}.");
            winningPlayer = null;
            return false;
        }
    }
}

