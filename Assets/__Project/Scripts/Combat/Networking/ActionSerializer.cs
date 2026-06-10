using Combat.Core;
using Combat.Config;
using Combat.Battlefield;
using System.Collections.Generic;
using System.Linq;

namespace Combat.Networking
{
    /// <summary>
    /// Serializes actions to network-transmittable format.
    /// </summary>
    public class ActionSerializer
    {
        public ActionData Serialize(IAction action)
        {
            var data = new ActionData
            {
                PlayerId = action.Player.Id,
                UnitId = action.UnitId,
                ActionType = action.Type
            };

            switch (action)
            {
                case MoveAction moveAction:
                    data.TargetQ = moveAction.TargetPosition.Q;
                    data.TargetR = moveAction.TargetPosition.R;
                    break;

                case ScheduleAbilityAction scheduleAction:
                    data.AbilityId = scheduleAction.AbilityId;
                    data.HasDirection = scheduleAction.Target.HasDirection;
                    if (data.HasDirection)
                        data.Direction = (int)scheduleAction.Target.Direction.Value;
                    break;

                case ReorderAbilitiesAction reorderAction:
                    data.NewOrder = reorderAction.NewOrder.ToArray();
                    break;

                case RetargetAbilityAction retargetAction:
                    data.AbilityIndexInQueue = retargetAction.AbilityIndexInQueue;
                    data.HasDirection = retargetAction.NewTarget.HasDirection;
                    if (data.HasDirection)
                        data.Direction = (int)retargetAction.NewTarget.Direction.Value;
                    break;
            }

            return data;
        }

        public IAction Deserialize(ActionData data, IPlayer player)
        {
            switch (data.ActionType)
            {
                case ActionType.Move:
                    return new MoveAction(
                        player,
                        data.UnitId,
                        new HexCoordinates(data.TargetQ, data.TargetR));

                case ActionType.ScheduleAbility:
                    return new ScheduleAbilityAction(
                        player,
                        data.UnitId,
                        data.AbilityId,
                        DeserializeAbilityTarget(data));

                case ActionType.ExecuteAbilityQueue:
                    return new ExecuteAbilityQueueAction(player, data.UnitId);

                case ActionType.ReorderAbilities:
                    return new ReorderAbilitiesAction(
                        player,
                        data.UnitId,
                        data.NewOrder.ToList());

                case ActionType.RetargetAbility:
                    return new RetargetAbilityAction(
                        player,
                        data.UnitId,
                        data.AbilityIndexInQueue,
                        DeserializeAbilityTarget(data));

                case ActionType.EndUnitTurn:
                    return new EndUnitTurnAction(player, data.UnitId);

                default:
                    throw new System.ArgumentException($"Unknown action type: {data.ActionType}");
            }
        }

        private static AbilityTarget DeserializeAbilityTarget(ActionData data)
        {
            if (data.HasDirection)
                return AbilityTarget.ForDirection((HexDirection)data.Direction);

            return AbilityTarget.None();
        }
    }
}
