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
        private readonly CombatConfig _combatConfig;

        public ActionValidator(CombatConfig combatConfig)
        {
            _combatConfig = combatConfig;
        }

        public bool Validate(ICombatState gameState, IAction action)
        {
            return ValidateDetailed(gameState, action).IsValid;
        }

        public ValidationResult ValidateDetailed(ICombatState gameState, IAction action)
        {
            // Player actions are only legal during the Act phase; enemy committed intents
            // resolve outside ProcessAction and never pass through this validator.
            if (gameState.RoundPhase != RoundPhase.PlayerAct)
            {
                return ValidationResult.Failure(
                    "Not the Act phase",
                    $"Round phase is {gameState.RoundPhase}; actions are only accepted during PlayerAct");
            }

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

            // Dead/stunned units can do nothing; a unit that has acted can still take
            // free actions (turning is free and unlimited up to executing the queue).
            if (unit.ActionState == UnitActionState.Dead || unit.ActionState == UnitActionState.Stunned)
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
                ActionType.ChangeDirection => ValidationResult.Success(),
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

            int maxRange = MovementRange.EffectiveFor(unit);
            if (maxRange == 0)
            {
                return ValidationResult.Failure(
                    "Unit cannot move",
                    $"Unit {action.UnitId} is rooted (movement range is 0)");
            }

            int distance = gameState.CalculateDistance(unit.Position, action.TargetPosition);
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

            return ValidationResult.Success();
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

    }
}
