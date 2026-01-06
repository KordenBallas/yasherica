using System.Collections.Generic;

namespace Combat.Core
{
    /// <summary>
    /// Action to reorder the abilities in the execution queue.
    /// This is a non-turn-ending action.
    /// </summary>
    public class ReorderAbilitiesAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.ReorderAbilities;
        public bool EndsTurn => false;
        
        /// <summary>
        /// New order of ability indices in the queue.
        /// </summary>
        public IReadOnlyList<int> NewOrder { get; }
        
        public ReorderAbilitiesAction(IPlayer player, int unitId, IReadOnlyList<int> newOrder)
        {
            Player = player;
            UnitId = unitId;
            NewOrder = newOrder;
        }
    }
}

