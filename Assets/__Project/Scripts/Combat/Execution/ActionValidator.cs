using Combat.Core;
using System.Linq;

namespace Combat.Execution
{
    /// <summary>
    /// Concrete implementation of action validator.
    /// Checks player ownership, unit state, and action-specific rules.
    /// </summary>
    public class ActionValidator : IActionValidator
    {
        public bool Validate(ICombatState gameState, IAction action)
        {
            return ValidateDetailed(gameState, action).IsValid;
        }
        
        public ValidationResult ValidateDetailed(ICombatState gameState, IAction action)
        {
            // Check that action player is current player
            if (action.Player.Id != gameState.CurrentPlayer.Id)
            {
                return ValidationResult.Failure(
                    "Not player's turn",
                    $"Action player {action.Player.Id} but current player is {gameState.CurrentPlayer.Id}");
            }
            
            // Get the unit
            var unit = gameState.GetUnit(action.UnitId);
            if (unit == null)
            {
                return ValidationResult.Failure(
                    "Unit not found",
                    $"Unit ID {action.UnitId} does not exist");
            }
            
            // Check unit ownership
            if (unit.Owner.Id != action.Player.Id)
            {
                return ValidationResult.Failure(
                    "Unit not owned by player",
                    $"Unit {action.UnitId} is owned by player {unit.Owner.Id}, not {action.Player.Id}");
            }
            
            // Check unit can act
            if (!unit.CanAct)
            {
                return ValidationResult.Failure(
                    "Unit cannot act",
                    $"Unit state: {unit.ActionState}");
            }
            
            // Check if unit has already acted this turn for turn-ending actions
            if (action.EndsTurn && unit.HasActedThisTurn)
            {
                return ValidationResult.Failure(
                    "Unit has already acted",
                    $"Unit {action.UnitId} has already performed its action this turn");
            }
            
            // Action-specific validation
            return action.Type switch
            {
                ActionType.Move => ValidateMoveAction(gameState, action as MoveAction),
                ActionType.ScheduleAbility => ValidateScheduleAbilityAction(gameState, action as ScheduleAbilityAction),
                ActionType.ExecuteAbilityQueue => ValidateExecuteAbilityQueueAction(gameState, action as ExecuteAbilityQueueAction),
                ActionType.ReorderAbilities => ValidateReorderAbilitiesAction(gameState, action as ReorderAbilitiesAction),
                ActionType.RetargetAbility => ValidateRetargetAbilityAction(gameState, action as RetargetAbilityAction),
                ActionType.EndUnitTurn => ValidationResult.Success(),
                _ => ValidationResult.Failure("Unknown action type")
            };
        }
        
        private ValidationResult ValidateMoveAction(ICombatState gameState, MoveAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);

            // Check if position is within battlefield boundaries
            if (!gameState.IsPositionValid(action.TargetPosition))
            {
                return ValidationResult.Failure(
                    "Position out of bounds",
                    $"Position {action.TargetPosition.Q},{action.TargetPosition.R} is outside battlefield boundaries");
            }

            // Check if target position is occupied
            var occupyingUnit = gameState.GetUnitAt(action.TargetPosition);
            if (occupyingUnit != null)
            {
                return ValidationResult.Failure(
                    "Position occupied",
                    $"Position {action.TargetPosition.Q},{action.TargetPosition.R} is occupied by unit {occupyingUnit.Id}");
            }

            // Check movement range (assume max range of 3 for now)
            int distance = gameState.CalculateDistance(unit.Position, action.TargetPosition);
            int maxRange = 3; // TODO: Get from unit movement stats when implemented

            if (distance > maxRange)
            {
                return ValidationResult.Failure(
                    "Move out of range",
                    $"Target position is {distance} hexes away, but max movement range is {maxRange}");
            }

            return ValidationResult.Success();
        }
        
        private ValidationResult ValidateScheduleAbilityAction(ICombatState gameState, ScheduleAbilityAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            
            // Get the ability
            var abilityInstance = unit.GetAbility(action.AbilityId);
            if (abilityInstance == null)
            {
                return ValidationResult.Failure(
                    "Ability not found",
                    $"Unit {action.UnitId} does not have ability {action.AbilityId}");
            }
            
            // Check if ability is available (not on cooldown)
            if (!abilityInstance.IsAvailable)
            {
                return ValidationResult.Failure(
                    "Ability on cooldown",
                    $"Ability {action.AbilityId} has {abilityInstance.CurrentCooldown} turns remaining");
            }
            
            // Check queue limit (max 1 new ability per turn)
            // This is a simplified check - in full implementation, would track scheduling history
            if (unit.AbilityQueue.Count >= 1)
            {
                return ValidationResult.Failure(
                    "Queue limit reached",
                    "Only 1 ability can be scheduled per turn");
            }
            
            // Validate target
            return ValidateAbilityTarget(gameState, unit, abilityInstance.Ability, action.Target);
        }
        
        private ValidationResult ValidateExecuteAbilityQueueAction(ICombatState gameState, ExecuteAbilityQueueAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            
            if (unit.AbilityQueue.Count == 0)
            {
                return ValidationResult.Failure(
                    "Empty ability queue",
                    "No abilities scheduled to execute");
            }
            
            return ValidationResult.Success();
        }
        
        private ValidationResult ValidateReorderAbilitiesAction(ICombatState gameState, ReorderAbilitiesAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            
            if (action.NewOrder.Count != unit.AbilityQueue.Count)
            {
                return ValidationResult.Failure(
                    "Invalid reorder",
                    $"New order has {action.NewOrder.Count} items but queue has {unit.AbilityQueue.Count}");
            }
            
            // Check all indices are valid
            for (int i = 0; i < action.NewOrder.Count; i++)
            {
                if (action.NewOrder[i] < 0 || action.NewOrder[i] >= unit.AbilityQueue.Count)
                {
                    return ValidationResult.Failure(
                        "Invalid index in reorder",
                        $"Index {action.NewOrder[i]} is out of range");
                }
            }
            
            return ValidationResult.Success();
        }
        
        private ValidationResult ValidateRetargetAbilityAction(ICombatState gameState, RetargetAbilityAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);
            
            if (action.AbilityIndexInQueue < 0 || action.AbilityIndexInQueue >= unit.AbilityQueue.Count)
            {
                return ValidationResult.Failure(
                    "Invalid queue index",
                    $"Index {action.AbilityIndexInQueue} is out of range (queue has {unit.AbilityQueue.Count} items)");
            }
            
            var scheduledAbility = unit.AbilityQueue[action.AbilityIndexInQueue];
            return ValidateAbilityTarget(gameState, unit, scheduledAbility.Ability.Ability, action.NewTarget);
        }
        
        private ValidationResult ValidateAbilityTarget(ICombatState gameState, IUnit caster, IAbility ability, AbilityTarget target)
        {
            // Validate target unit exists if targeting a unit
            if (target.TargetUnitId.HasValue)
            {
                var targetUnit = gameState.GetUnit(target.TargetUnitId.Value);
                if (targetUnit == null)
                {
                    return ValidationResult.Failure(
                        "Target unit not found",
                        $"Unit ID {target.TargetUnitId.Value} does not exist");
                }
                
                // Check target is alive
                if (!targetUnit.IsAlive)
                {
                    return ValidationResult.Failure(
                        "Target is dead",
                        $"Cannot target dead unit {target.TargetUnitId.Value}");
                }
                
                // Validate target type matches ability requirements
                if (ability.TargetType == AbilityTargetType.Enemy && targetUnit.Owner.Id == caster.Owner.Id)
                {
                    return ValidationResult.Failure(
                        "Invalid target",
                        "Ability requires enemy target but target is ally");
                }
                
                if (ability.TargetType == AbilityTargetType.Ally && targetUnit.Owner.Id != caster.Owner.Id)
                {
                    return ValidationResult.Failure(
                        "Invalid target",
                        "Ability requires ally target but target is enemy");
                }

                // Range validation
                int distance = gameState.CalculateDistance(caster.Position, targetUnit.Position);
                if (distance > ability.Range)
                {
                    return ValidationResult.Failure(
                        "Target out of range",
                        $"Target is {distance} hexes away, but ability range is {ability.Range}");
                }
            }

            return ValidationResult.Success();
        }
    }
}

