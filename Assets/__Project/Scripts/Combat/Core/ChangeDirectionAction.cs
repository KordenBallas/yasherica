using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Action to change the direction a unit is facing.
    /// Free action by default (does not end turn).
    /// </summary>
    public class ChangeDirectionAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.ChangeDirection;
        public bool EndsTurn => false; // Free action by default
        public HexCoordinates NewFacingDirection { get; }

        public ChangeDirectionAction(IPlayer player, int unitId, HexCoordinates newFacingDirection)
        {
            Player = player;
            UnitId = unitId;
            NewFacingDirection = newFacingDirection;
        }
    }
}
