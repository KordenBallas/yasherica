using Combat.Config;
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
        private readonly HexDirectionConfig _hexDirectionConfig;
        private readonly CombatConfig _combatConfig;

        public ActionValidator(HexDirectionConfig hexDirectionConfig, CombatConfig combatConfig)
        {
            _hexDirectionConfig = hexDirectionConfig;
            _combatConfig = combatConfig;
        }

        public bool Validate(ICombatState gameState, IAction action)
        {
            return ValidateDetailed(gameState, action).IsValid;
        }

        public ValidationResult ValidateDetailed(ICombatState gameState, IAction action)
        {
            if (action.Player.Id != gameState.CurrentPlayer.Id)
            {
                return ValidationResult.Failure(
                    "Not player's turn",
                    $"Action player {action.Player.Id} but current player is {gameState.CurrentPlayer.Id}");
            }

            var unit = gameState.GetUnit(action.UnitId);
            if (unit == null)
            {
                return ValidationResult.Failure(
                    "Unit not found",
                    $"Unit ID {action.UnitId} does not exist");
            }

            if (unit.Owner.Id != action.Player.Id)
            {
                return ValidationResult.Failure(
                    "Unit not owned by player",
                    $"Unit {action.UnitId} is owned by player {unit.Owner.Id}, not {action.Player.Id}");
            }

            if (!unit.CanAct)
            {
                return ValidationResult.Failure(
                    "Unit cannot act",
                    $"Unit state: {unit.ActionState}");
            }

            if (action.EndsTurn && unit.HasActedThisTurn)
            {
                return ValidationResult.Failure(
                    "Unit has already acted",
                    $"Unit {action.UnitId} has already performed its action this turn");
            }

            return action.Type switch
            {
                ActionType.Move => ValidateMoveAction(gameState, action as MoveAction),
                ActionType.ScheduleAbility => ValidateScheduleAbilityAction(gameState, action as ScheduleAbilityAction),
                ActionType.ExecuteAbilityQueue => ValidateExecuteAbilityQueueAction(gameState, action as ExecuteAbilityQueueAction),
                ActionType.ReorderAbilities => ValidateReorderAbilitiesAction(gameState, action as ReorderAbilitiesAction),
                ActionType.RetargetAbility => ValidateRetargetAbilityAction(gameState, action as RetargetAbilityAction),
                ActionType.ChangeDirection => ValidateChangeDirectionAction(gameState, action as ChangeDirectionAction),
                ActionType.EndUnitTurn => ValidationResult.Success(),
                _ => ValidationResult.Failure("Unknown action type")
            };
        }

        private ValidationResult ValidateMoveAction(ICombatState gameState, MoveAction action)
        {
            var unit = gameState.GetUnit(action.UnitId);

            if (!gameState.IsPositionValid(action.TargetPosition))
            {
                return ValidationResult.Failure(
                    "Position out of bounds",
                    $"Position {action.TargetPosition.Q},{action.TargetPosition.R} is outside battlefield boundaries");
            }

            var occupyingUnit = gameState.GetUnitAt(action.TargetPosition);
            if (occupyingUnit != null)
            {
                return ValidationResult.Failure(
                    "Position occupied",
                    $"Position {action.TargetPosition.Q},{action.TargetPosition.R} is occupied by unit {occupyingUnit.Id}");
            }

            int distance = gameState.CalculateDistance(unit.Position, action.TargetPosition);
            int maxRange = 3;

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

            var abilityInstance = unit.GetAbility(action.AbilityId);
            if (abilityInstance == null)
            {
                return ValidationResult.Failure(
                    "Ability not found",
                    $"Unit {action.UnitId} does not have ability {action.AbilityId}");
            }

            if (!abilityInstance.IsAvailable)
            {
                return ValidationResult.Failure(
                    "Ability on cooldown",
                    $"Ability {action.AbilityId} has {abilityInstance.CurrentCooldown} turns remaining");
            }

            if (_combatConfig.MaxAbilityQueueSize > 0 && unit.AbilityQueue.Count >= _combatConfig.MaxAbilityQueueSize)
            {
                return ValidationResult.Failure(
                    "Queue limit reached",
                    $"Maximum queue size is {_combatConfig.MaxAbilityQueueSize}");
            }

            return ValidateAbilityTarget(abilityInstance.Ability, action.Target);
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
            return ValidateAbilityTarget(scheduledAbility.Ability.Ability, action.NewTarget);
        }

        private ValidationResult ValidateChangeDirectionAction(ICombatState gameState, ChangeDirectionAction action)
        {
            bool isValidDirection = false;
            foreach (var directionOffset in _hexDirectionConfig.directionOffsets)
            {
                if (directionOffset.offset.x == action.NewFacingDirection.Q &&
                    directionOffset.offset.y == action.NewFacingDirection.R)
                {
                    isValidDirection = true;
                    break;
                }
            }

            if (!isValidDirection)
            {
                return ValidationResult.Failure(
                    "Invalid facing direction",
                    $"Direction ({action.NewFacingDirection.Q},{action.NewFacingDirection.R}) is not a valid hex neighbor offset");
            }

            return ValidationResult.Success();
        }

        private static ValidationResult ValidateAbilityTarget(IAbility ability, AbilityTarget target)
        {
            if (ability.Shape.Type == AbilityShapeType.Line && !target.HasDirection)
            {
                return ValidationResult.Failure(
                    "Missing direction",
                    "Line ability requires a direction to be chosen");
            }

            return ValidationResult.Success();
        }
    }
}
