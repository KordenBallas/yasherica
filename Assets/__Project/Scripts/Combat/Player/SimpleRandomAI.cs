using Combat.Core;
using Combat.Config;
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

        public SimpleRandomAI(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public IAction DecideAction(ICombatState gameState, IUnit unit)
        {
            var validActions = GetValidActions(gameState, unit);

            if (validActions.Count == 0)
                return new EndUnitTurnAction(unit.Owner, unit.Id);

            int index = _random.Next(validActions.Count);
            IAction action = validActions[index];
            Debug.Log($"[SimpleRandomAI] Decided to perform {action.Type}.");
            return action;
        }

        private List<IAction> GetValidActions(ICombatState gameState, IUnit unit)
        {
            var actions = new List<IAction>();

            foreach (var abilityInstance in unit.GetAvailableAbilities())
            {
                var ability = abilityInstance.Ability;

                foreach (var target in GetTargetsForAbility(ability))
                {
                    actions.Add(new ScheduleAbilityAction(unit.Owner, unit.Id, ability.Id, target));
                }
            }

            foreach (var position in gameState.GetValidPositionsInRange(unit.Position, 1))
            {
                actions.Add(new MoveAction(unit.Owner, unit.Id, position));
            }

            actions.Add(new EndUnitTurnAction(unit.Owner, unit.Id));

            Debug.Log($"[SimpleRandomAI] Identified {actions.Count} valid actions.");
            return actions;
        }

        private static IEnumerable<AbilityTarget> GetTargetsForAbility(IAbility ability)
        {
            if (ability.Shape.Type == AbilityShapeType.Ring)
            {
                yield return AbilityTarget.None();
            }
            else
            {
                foreach (var dir in AllDirections)
                    yield return AbilityTarget.ForDirection(dir);
            }
        }
    }
}
