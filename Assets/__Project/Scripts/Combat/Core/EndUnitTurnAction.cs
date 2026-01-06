namespace Combat.Core
{
    /// <summary>
    /// Action to explicitly end a unit's turn without performing any action.
    /// This is a turn-ending action.
    /// </summary>
    public class EndUnitTurnAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.EndUnitTurn;
        public bool EndsTurn => true;
        
        public EndUnitTurnAction(IPlayer player, int unitId)
        {
            Player = player;
            UnitId = unitId;
        }
    }
}

