using Combat.Config;

namespace Combat.Core
{
    /// <summary>
    /// Action to change the direction a unit is facing.
    /// Free action (does not end turn) — turning is unlimited during the player's Act phase,
    /// and the whole queued volley fires along the final facing.
    /// </summary>
    public class ChangeDirectionAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.ChangeDirection;
        public bool EndsTurn => false;
        public HexDirection NewFacing { get; }

        public ChangeDirectionAction(IPlayer player, int unitId, HexDirection newFacing)
        {
            Player = player;
            UnitId = unitId;
            NewFacing = newFacing;
        }
    }
}
