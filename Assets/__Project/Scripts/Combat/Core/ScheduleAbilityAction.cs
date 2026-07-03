using Combat.Config;

namespace Combat.Core
{
    /// <summary>
    /// Action to schedule an ability for later execution.
    /// This is a turn-ending action.
    /// Directional abilities carry no per-ability direction — they fire along the unit's
    /// facing at execution time. FacingToSet lets the AI turn-and-schedule in one action.
    /// </summary>
    public class ScheduleAbilityAction : IAction
    {
        public IPlayer Player { get; }
        public int UnitId { get; }
        public ActionType Type => ActionType.ScheduleAbility;
        public bool EndsTurn => true;

        /// <summary>
        /// ID of the ability to schedule.
        /// </summary>
        public int AbilityId { get; }

        /// <summary>
        /// Optional facing applied to the unit before enqueuing (AI schedule-and-face).
        /// Null for the player — the player's facing is set by free ChangeDirectionActions.
        /// </summary>
        public HexDirection? FacingToSet { get; }

        public ScheduleAbilityAction(IPlayer player, int unitId, int abilityId, HexDirection? facingToSet = null)
        {
            Player = player;
            UnitId = unitId;
            AbilityId = abilityId;
            FacingToSet = facingToSet;
        }
    }
}
