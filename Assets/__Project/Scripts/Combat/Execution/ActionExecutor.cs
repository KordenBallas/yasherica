using Combat.Core;
using System.Linq;
using UnityEngine;

namespace Combat.Execution
{
    /// <summary>
    /// Concrete implementation of action executor.
    /// Dispatches to appropriate handler based on action type.
    /// </summary>
    public class ActionExecutor : IActionExecutor
    {
        private readonly IAbilityExecutor _abilityExecutor;
        
        public ActionExecutor(IAbilityExecutor abilityExecutor)
        {
            _abilityExecutor = abilityExecutor;
        }
        
        public ICombatState Execute(ICombatState gameState, IAction action)
        {
            return ExecuteWithResult(gameState, action).NewState;
        }
        
        public ActionResult ExecuteWithResult(ICombatState gameState, IAction action)
        {
            var newState = action.Type switch
            {
                ActionType.Move => ExecuteMove(gameState, action as MoveAction),
                ActionType.ScheduleAbility => ExecuteScheduleAbility(gameState, action as ScheduleAbilityAction),
                ActionType.ExecuteAbilityQueue => ExecuteAbilityQueue(gameState, action as ExecuteAbilityQueueAction),
                ActionType.ReorderAbilities => ExecuteReorderAbilities(gameState, action as ReorderAbilitiesAction),
                ActionType.RetargetAbility => ExecuteRetargetAbility(gameState, action as RetargetAbilityAction),
                ActionType.EndUnitTurn => ExecuteEndUnitTurn(gameState, action as EndUnitTurnAction),
                _ => gameState
            };
            
            // If action ends turn, set HasActedThisTurn
            if (action.EndsTurn)
            {
                var unit = newState.GetUnit(action.UnitId);
                if (unit == null)
                {
                    Debug.LogError($"[ActionExecutor] Cannot set HasActedThisTurn: Unit {action.UnitId} not found in state after executing {action.Type}");
                    return ActionResult.Successful(newState);
                }

                Debug.Log($"[ActionExecutor] Terminal action executed - Setting Unit {action.UnitId} HasActedThisTurn=true (Action: {action.Type})");

                var updatedUnit = (unit as Unit).WithActedThisTurn(true);
                newState = (newState as CombatState).WithUpdatedUnit(updatedUnit);

                // Verify the update worked
                var verifyUnit = newState.GetUnit(action.UnitId);
                Debug.Log($"[ActionExecutor] Verification - Unit {action.UnitId} HasActedThisTurn={verifyUnit?.HasActedThisTurn}, CanAct={verifyUnit?.CanAct}");
            }
            
            return ActionResult.Successful(newState);
        }
        
        private ICombatState ExecuteMove(ICombatState gameState, MoveAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            if (unit == null)
            {
                Debug.LogError($"[ActionExecutor] Cannot execute MoveAction: Unit {action.UnitId} not found in combat state. Action: Move to {action.TargetPosition}");
                return gameState;
            }
            var updatedUnit = (unit as Unit).WithPosition(action.TargetPosition);
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
        }
        
        private ICombatState ExecuteScheduleAbility(ICombatState gameState, ScheduleAbilityAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            if (unit == null)
            {
                Debug.LogError($"[ActionExecutor] Cannot execute ScheduleAbilityAction: Unit {action.UnitId} not found in combat state. Ability ID: {action.AbilityId}");
                return gameState;
            }
            var abilityInstance = unit.GetAbility(action.AbilityId);
            
            if (abilityInstance == null)
                return gameState;
            
            // Add to queue
            var scheduledAbility = new ScheduledAbility(
                abilityInstance,
                action.Target,
                unit.AbilityQueue.Count // Execution order
            );
            
            var newQueue = unit.AbilityQueue.Concat(new[] { scheduledAbility }).ToList();
            var updatedUnit = (unit as Unit).WithAbilityQueue(newQueue);
            
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
        }
        
        private ICombatState ExecuteAbilityQueue(ICombatState gameState, ExecuteAbilityQueueAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            if (unit == null)
            {
                Debug.LogError($"[ActionExecutor] Cannot execute ExecuteAbilityQueueAction: Unit {action.UnitId} not found in combat state");
                return gameState;
            }
            var newState = gameState;
            
            // Execute all abilities in order
            foreach (var scheduledAbility in unit.AbilityQueue.OrderBy(a => a.ExecutionOrder))
            {
                newState = _abilityExecutor.ExecuteAbility(newState, unit, scheduledAbility);
            }
            
            // Reset cooldowns for executed abilities
            var updatedUnit = newState.GetUnit(action.UnitId) as Unit;
            if (updatedUnit == null)
            {
                Debug.LogError($"[ActionExecutor] Cannot update cooldowns: Unit {action.UnitId} not found in state after ability execution");
                return newState;
            }
            var newAbilities = updatedUnit.Abilities.Select(a =>
            {
                // If ability was in queue, reset its cooldown
                if (unit.AbilityQueue.Any(sa => sa.Ability.Ability.Id == a.Ability.Id))
                {
                    return (a as AbilityInstance).ResetCooldown();
                }
                return a;
            }).ToList();
            
            updatedUnit = updatedUnit.WithAbilities(newAbilities);
            
            // Clear queue
            updatedUnit = updatedUnit.WithAbilityQueue(new System.Collections.Generic.List<ScheduledAbility>());
            
            return (newState as CombatState).WithUpdatedUnit(updatedUnit);
        }
        
        private ICombatState ExecuteReorderAbilities(ICombatState gameState, ReorderAbilitiesAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            if (unit == null)
            {
                Debug.LogError($"[ActionExecutor] Cannot execute ReorderAbilitiesAction: Unit {action.UnitId} not found in combat state");
                return gameState;
            }
            
            // Reorder queue based on new indices
            var newQueue = action.NewOrder
                .Select((originalIndex, newIndex) => new ScheduledAbility(
                    unit.AbilityQueue[originalIndex].Ability,
                    unit.AbilityQueue[originalIndex].Target,
                    newIndex
                ))
                .ToList();
            
            var updatedUnit = (unit as Unit).WithAbilityQueue(newQueue);
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
        }
        
        private ICombatState ExecuteRetargetAbility(ICombatState gameState, RetargetAbilityAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            if (unit == null)
            {
                Debug.LogError($"[ActionExecutor] Cannot execute RetargetAbilityAction: Unit {action.UnitId} not found in combat state. Ability index: {action.AbilityIndexInQueue}");
                return gameState;
            }
            
            // Update target for specified ability
            var newQueue = unit.AbilityQueue
                .Select((sa, index) =>
                {
                    if (index == action.AbilityIndexInQueue)
                    {
                        return new ScheduledAbility(sa.Ability, action.NewTarget, sa.ExecutionOrder);
                    }
                    return sa;
                })
                .ToList();
            
            var updatedUnit = (unit as Unit).WithAbilityQueue(newQueue);
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
        }
        
        private ICombatState ExecuteEndUnitTurn(ICombatState gameState, EndUnitTurnAction action)
        {
            // No state change except HasActedThisTurn (handled in ExecuteWithResult)
            return gameState;
        }
    }
}

