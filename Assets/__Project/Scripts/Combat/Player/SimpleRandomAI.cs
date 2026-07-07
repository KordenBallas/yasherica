using Combat.Core;
using Combat.Config;
using Core.Logging;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Simple AI that picks random valid actions.
    /// Used for testing — no tactical evaluation.
    /// </summary>
    public class SimpleRandomAI : IAIDecisionMaker
    {
        private static readonly HexDirection[] AllDirections =
        {
            HexDirection.E, HexDirection.NE, HexDirection.NW,
            HexDirection.W, HexDirection.SW, HexDirection.SE
        };

        private readonly System.Random _random;
        private readonly IGameLogger _logger;

        public SimpleRandomAI(int? seed = null, IGameLogger logger = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            _logger = logger;
        }

        public IAction DecideAction(ICombatState gameState, IUnit unit)
        {
            var validActions = GetValidActions(gameState, unit);

            if (validActions.Count == 0)
                return new EndUnitTurnAction(unit.Owner, unit.Id);

            int index = _random.Next(validActions.Count);
            IAction action = validActions[index];
            _logger?.Info(LogCategory.Combat,$"[SimpleRandomAI] Decided to perform {action.Type}.");
            return action;
        }

        private List<IAction> GetValidActions(ICombatState gameState, IUnit unit)
        {
            var actions = new List<IAction>();

            foreach (var abilityInstance in unit.GetAvailableAbilities())
            {
                var ability = abilityInstance.Ability;

                foreach (var facing in GetFacingsForAbility(ability))
                {
                    actions.Add(new ScheduleAbilityAction(unit.Owner, unit.Id, ability.Id, facing));
                }
            }

            foreach (var position in gameState.GetValidPositionsInRange(unit.Position, MovementRange.EffectiveFor(unit, 1)))
            {
                actions.Add(new MoveAction(unit.Owner, unit.Id, position));
            }

            actions.Add(new EndUnitTurnAction(unit.Owner, unit.Id));

            _logger?.Info(LogCategory.Combat,$"[SimpleRandomAI] Identified {actions.Count} valid actions.");
            return actions;
        }

        private static IEnumerable<HexDirection?> GetFacingsForAbility(IAbility ability)
        {
            if (ability.Shape.Type == AbilityShapeType.Ring)
            {
                yield return null; // Ring ignores facing
            }
            else
            {
                foreach (var dir in AllDirections)
                    yield return dir;
            }
        }
    }
}
