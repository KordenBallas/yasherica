using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Action to move a unit to a different position.
    /// This is a turn-ending action.
    /// </summary>
    public class MoveAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.Move;
        public bool EndsTurn => true;
        
        /// <summary>
        /// Target position to move to.
        /// </summary>
        public HexCoordinates TargetPosition { get; }
        
        public MoveAction(IPlayer player, int unitId, HexCoordinates targetPosition)
        {
            Player = player;
            UnitId = unitId;
            TargetPosition = targetPosition;
        }
    }
}

