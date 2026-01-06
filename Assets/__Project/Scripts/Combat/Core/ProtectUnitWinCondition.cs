using Combat.Core;

namespace Combat.Core
{
    /// <summary>
    /// Win condition: Keep a specific VIP unit alive.
    /// If the VIP dies, the player loses.
    /// </summary>
    public class ProtectUnitWinCondition : IWinCondition
    {
        public WinConditionType Type => WinConditionType.ProtectUnit;
        
        private readonly int _vipUnitId;
        
        public ProtectUnitWinCondition(int vipUnitId)
        {
            _vipUnitId = vipUnitId;
        }
        
        public bool Check(ICombatState gameState, out IPlayer winningPlayer)
        {
            var vipUnit = gameState.GetUnit(_vipUnitId);
            
            if (vipUnit == null || !vipUnit.IsAlive)
            {
                // VIP is dead - find other players as potential winners
                foreach (var player in gameState.Players)
                {
                    if (vipUnit != null && player.Id != vipUnit.Owner.Id)
                    {
                        var hasAliveUnits = false;
                        foreach (var unit in gameState.GetUnitsByPlayer(player))
                        {
                            if (unit.IsAlive)
                            {
                                hasAliveUnits = true;
                                break;
                            }
                        }
                        
                        if (hasAliveUnits)
                        {
                            winningPlayer = player;
                            return true;
                        }
                    }
                }
            }
            
            winningPlayer = null;
            return false;
        }
    }
}

