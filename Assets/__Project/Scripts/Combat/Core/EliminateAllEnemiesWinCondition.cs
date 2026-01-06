using System.Linq;

namespace Combat.Core
{
    /// <summary>
    /// Win condition: Eliminate all enemy units.
    /// The last player with alive units wins.
    /// </summary>
    public class EliminateAllEnemiesWinCondition : IWinCondition
    {
        public WinConditionType Type => WinConditionType.EliminateAllEnemies;
        
        public bool Check(ICombatState gameState, out IPlayer winningPlayer)
        {
            winningPlayer = null;
            
            // Find all players with alive units
            var playersWithAliveUnits = gameState.Players
                .Where(p => gameState.GetUnitsByPlayer(p).Any(u => u.IsAlive))
                .ToList();
            
            // If only one player has alive units, they win
            if (playersWithAliveUnits.Count == 1)
            {
                winningPlayer = playersWithAliveUnits[0];
                return true;
            }
            
            return false;
        }
    }
}

