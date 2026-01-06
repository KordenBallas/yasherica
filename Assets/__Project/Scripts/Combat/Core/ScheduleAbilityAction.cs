namespace Combat.Core
{
    /// <summary>
    /// Action to schedule an ability for later execution.
    /// This is a non-turn-ending action (allows planning).
    /// </summary>
    public class ScheduleAbilityAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.ScheduleAbility;
        public bool EndsTurn => false;
        
        /// <summary>
        /// ID of the ability to schedule.
        /// </summary>
        public int AbilityId { get; }
        
        /// <summary>
        /// Target for the ability.
        /// </summary>
        public AbilityTarget Target { get; }
        
        public ScheduleAbilityAction(IPlayer player, int unitId, int abilityId, AbilityTarget target)
        {
            Player = player;
            UnitId = unitId;
            AbilityId = abilityId;
            Target = target;
        }
    }
}

