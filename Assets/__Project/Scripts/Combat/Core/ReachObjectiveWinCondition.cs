using Combat.Core;
using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Win condition: Reach a specific objective position.
    /// First player to move a unit to the target wins.
    /// </summary>
    public class ReachObjectiveWinCondition : IWinCondition
    {
        public WinConditionType Type => WinConditionType.ReachObjective;
        
        private readonly HexCoordinates _objectivePosition;
        
        public ReachObjectiveWinCondition(HexCoordinates objectivePosition)
        {
            _objectivePosition = objectivePosition;
        }
        
        public bool Check(ICombatState gameState, out IPlayer winningPlayer)
        {
            var unitAtObjective = gameState.GetUnitAt(_objectivePosition);
            
            if (unitAtObjective != null && unitAtObjective.IsAlive)
            {
                winningPlayer = unitAtObjective.Owner;
                return true;
            }
            
            winningPlayer = null;
            return false;
        }
    }
}

