namespace Combat.Core
{
    /// <summary>
    /// Action to change the target of a scheduled ability in the queue.
    /// This is a non-turn-ending action.
    /// </summary>
    public class RetargetAbilityAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.RetargetAbility;
        public bool EndsTurn => false;
        
        /// <summary>
        /// Index of the ability in the queue to retarget.
        /// </summary>
        public int AbilityIndexInQueue { get; }
        
        /// <summary>
        /// New target for the ability.
        /// </summary>
        public AbilityTarget NewTarget { get; }
        
        public RetargetAbilityAction(IPlayer player, int unitId, int abilityIndexInQueue, AbilityTarget newTarget)
        {
            Player = player;
            UnitId = unitId;
            AbilityIndexInQueue = abilityIndexInQueue;
            NewTarget = newTarget;
        }
    }
}

