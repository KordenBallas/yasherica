namespace Combat.Core
{
    /// <summary>
    /// Action to execute all scheduled abilities in the unit's queue.
    /// This is a turn-ending action.
    /// </summary>
    public class ExecuteAbilityQueueAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.ExecuteAbilityQueue;
        public bool EndsTurn => true;
        
        public ExecuteAbilityQueueAction(IPlayer player, int unitId)
        {
            Player = player;
            UnitId = unitId;
        }
    }
}

