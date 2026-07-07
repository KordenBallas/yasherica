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
    /// Advanced tactical AI that evaluates damage, positioning, and threat.
    /// </summary>
    public class TacticalAI : IAIDecisionMaker
    {
        private static readonly HexDirection[] AllDirections =
        {
            HexDirection.E, HexDirection.NE, HexDirection.NW,
            HexDirection.W, HexDirection.SW, HexDirection.SE
        };

        private readonly System.Random _random;
        private readonly IGameLogger _logger;

        public TacticalAI(int? seed = null, IGameLogger logger = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            _logger = logger;
        }

        public IAction DecideAction(ICombatState gameState, IUnit unit)
        {
            var scoredActions = EvaluateAllActions(gameState, unit);

            if (scoredActions.Count == 0)
                return new EndUnitTurnAction(unit.Owner, unit.Id);

            var bestAction = scoredActions.OrderByDescending(sa => sa.Score).First();
            _logger?.Info(LogCategory.Combat,$"[TacticalAI] Decided to perform {bestAction.Action.Type}.");
            return bestAction.Action;
        }

        private List<ScoredAction> EvaluateAllActions(ICombatState gameState, IUnit unit)
        {
            var scoredActions = new List<ScoredAction>();

            foreach (var abilityInstance in unit.GetAvailableAbilities())
            {
                var ability = abilityInstance.Ability;

                foreach (var facing in GetFacingsForAbility(ability))
                {
                    float score = EvaluateAbilityAction(ability);
                    scoredActions.Add(new ScoredAction(
                        new ScheduleAbilityAction(unit.Owner, unit.Id, ability.Id, facing),
                        score));
                }
            }

            foreach (var position in gameState.GetValidPositionsInRange(unit.Position, MovementRange.EffectiveFor(unit)))
            {
                float score = EvaluatePosition(gameState, unit, position);
                scoredActions.Add(new ScoredAction(new MoveAction(unit.Owner, unit.Id, position), score));
            }

            scoredActions.Add(new ScoredAction(new EndUnitTurnAction(unit.Owner, unit.Id), 10f));
            _logger?.Info(LogCategory.Combat,$"[TacticalAI] Identified {scoredActions.Count} valid actions.");
            return scoredActions;
        }

        private static float EvaluateAbilityAction(IAbility ability)
        {
            float score = 50f;

            if (ability is IDamageAbility damageAbility)
                score += damageAbility.Damage * 2f;

            if (ability is IHealAbility healAbility)
                score += healAbility.HealAmount;

            if (ability is IStatusEffectAbility)
                score += 30f;

            return score;
        }

        private float EvaluatePosition(ICombatState gameState, IUnit unit, HexCoordinates position)
        {
            float score = 30f;

            var enemies = gameState.Units.Where(u => u.Owner.Id != unit.Owner.Id && u.IsAlive).ToList();
            if (enemies.Count == 0) return score;

            var closestEnemy = enemies.OrderBy(e => CalculateDistance(position, e.Position)).First();
            int dist = CalculateDistance(position, closestEnemy.Position);
            float hpPercent = (float)unit.CurrentHP / unit.MaxHP;

            score += hpPercent > 0.5f
                ? (10 - dist) * 5f
                : dist * 3f;

            int enemiesNearby = enemies.Count(e => CalculateDistance(position, e.Position) <= 2);
            score -= enemiesNearby * 15f;

            return score;
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

        private static int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            int dq = System.Math.Abs(from.Q - to.Q);
            int dr = System.Math.Abs(from.R - to.R);
            int ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));
            return (dq + dr + ds) / 2;
        }

        private struct ScoredAction
        {
            public IAction Action;
            public float Score;

            public ScoredAction(IAction action, float score)
            {
                Action = action;
                Score = score;
            }
        }
    }
}
