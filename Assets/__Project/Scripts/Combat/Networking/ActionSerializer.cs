using Combat.Core;
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
        /// <summary>
        /// Serializes an action to ActionData.
        /// </summary>
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
                    data.AbilityTargetType = (int)scheduleAction.Target.Type;
                    data.HasTargetUnit = scheduleAction.Target.TargetUnitId.HasValue;
                    data.HasTargetPosition = scheduleAction.Target.TargetPosition.HasValue;
                    
                    if (data.HasTargetUnit)
                        data.TargetUnitId = scheduleAction.Target.TargetUnitId.Value;
                    
                    if (data.HasTargetPosition)
                    {
                        data.TargetPositionQ = scheduleAction.Target.TargetPosition.Value.Q;
                        data.TargetPositionR = scheduleAction.Target.TargetPosition.Value.R;
                    }
                    break;
                    
                case ReorderAbilitiesAction reorderAction:
                    data.NewOrder = reorderAction.NewOrder.ToArray();
                    break;
                    
                case RetargetAbilityAction retargetAction:
                    data.AbilityIndexInQueue = retargetAction.AbilityIndexInQueue;
                    data.AbilityTargetType = (int)retargetAction.NewTarget.Type;
                    data.HasTargetUnit = retargetAction.NewTarget.TargetUnitId.HasValue;
                    data.HasTargetPosition = retargetAction.NewTarget.TargetPosition.HasValue;
                    
                    if (data.HasTargetUnit)
                        data.TargetUnitId = retargetAction.NewTarget.TargetUnitId.Value;
                    
                    if (data.HasTargetPosition)
                    {
                        data.TargetPositionQ = retargetAction.NewTarget.TargetPosition.Value.Q;
                        data.TargetPositionR = retargetAction.NewTarget.TargetPosition.Value.R;
                    }
                    break;
            }
            
            return data;
        }
        
        /// <summary>
        /// Deserializes ActionData back to an IAction.
        /// Requires player lookup.
        /// </summary>
        public IAction Deserialize(ActionData data, IPlayer player)
        {
            switch (data.ActionType)
            {
                case ActionType.Move:
                    return new MoveAction(
                        player,
                        data.UnitId,
                        new HexCoordinates(data.TargetQ, data.TargetR)
                    );
                    
                case ActionType.ScheduleAbility:
                    var target = DeserializeAbilityTarget(data);
                    return new ScheduleAbilityAction(
                        player,
                        data.UnitId,
                        data.AbilityId,
                        target
                    );
                    
                case ActionType.ExecuteAbilityQueue:
                    return new ExecuteAbilityQueueAction(player, data.UnitId);
                    
                case ActionType.ReorderAbilities:
                    return new ReorderAbilitiesAction(
                        player,
                        data.UnitId,
                        data.NewOrder.ToList()
                    );
                    
                case ActionType.RetargetAbility:
                    var newTarget = DeserializeAbilityTarget(data);
                    return new RetargetAbilityAction(
                        player,
                        data.UnitId,
                        data.AbilityIndexInQueue,
                        newTarget
                    );
                    
                case ActionType.EndUnitTurn:
                    return new EndUnitTurnAction(player, data.UnitId);
                    
                default:
                    throw new System.ArgumentException($"Unknown action type: {data.ActionType}");
            }
        }
        
        private AbilityTarget DeserializeAbilityTarget(ActionData data)
        {
            var targetType = (AbilityTargetType)data.AbilityTargetType;
            
            if (targetType == AbilityTargetType.Self)
            {
                return AbilityTarget.ForSelf();
            }
            else if (data.HasTargetUnit)
            {
                return AbilityTarget.ForUnit(data.TargetUnitId, targetType);
            }
            else if (data.HasTargetPosition)
            {
                return AbilityTarget.ForPosition(
                    new HexCoordinates(data.TargetPositionQ, data.TargetPositionR),
                    targetType
                );
            }
            
            throw new System.ArgumentException("Invalid ability target data");
        }
    }
}

