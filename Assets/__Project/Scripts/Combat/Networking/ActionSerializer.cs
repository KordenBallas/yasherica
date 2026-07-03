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
                    data.HasDirection = scheduleAction.FacingToSet.HasValue;
                    if (data.HasDirection)
                        data.Direction = (int)scheduleAction.FacingToSet.Value;
                    break;

                case ReorderAbilitiesAction reorderAction:
                    data.NewOrder = reorderAction.NewOrder.ToArray();
                    break;

                case ChangeDirectionAction changeDirectionAction:
                    data.HasDirection = true;
                    data.Direction = (int)changeDirectionAction.NewFacing;
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
                        data.HasDirection ? (HexDirection)data.Direction : (HexDirection?)null);

                case ActionType.ExecuteAbilityQueue:
                    return new ExecuteAbilityQueueAction(player, data.UnitId);

                case ActionType.ReorderAbilities:
                    return new ReorderAbilitiesAction(
                        player,
                        data.UnitId,
                        data.NewOrder.ToList());

                case ActionType.ChangeDirection:
                    return new ChangeDirectionAction(
                        player,
                        data.UnitId,
                        (HexDirection)data.Direction);

                case ActionType.EndUnitTurn:
                    return new EndUnitTurnAction(player, data.UnitId);

                default:
                    throw new System.ArgumentException($"Unknown action type: {data.ActionType}");
            }
        }
    }
}
