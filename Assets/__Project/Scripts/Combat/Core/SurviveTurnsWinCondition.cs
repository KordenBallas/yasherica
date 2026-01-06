using Combat.Core;
using System.Linq;

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
        
        public SurviveTurnsWinCondition(int targetTurns, int targetPlayerId)
        {
            _targetTurns = targetTurns;
            _targetPlayerId = targetPlayerId;
        }
        
        public bool Check(ICombatState gameState, out IPlayer winningPlayer)
        {
            if (gameState.TurnNumber >= _targetTurns)
            {
                // Find the target player
                winningPlayer = gameState.Players.FirstOrDefault(p => p.Id == _targetPlayerId);
                
                // Check if they have any alive units
                if (winningPlayer != null)
                {
                    var aliveUnits = gameState.GetUnitsByPlayer(winningPlayer).Where(u => u.IsAlive);
                    if (aliveUnits.Any())
                    {
                        return true;
                    }
                }
            }
            
            winningPlayer = null;
            return false;
        }
    }
}

