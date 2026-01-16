using Combat.Core;
using Combat.Data.Definitions;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Tactical AI with configurable parameters from AIProfileDefinition.
    /// Extends base tactical behavior with data-driven scoring weights.
    /// </summary>
    public class ConfigurableTacticalAI : IAIDecisionMaker
    {
        private readonly AIProfileDefinition _profile;
        private readonly System.Random _random;

        public ConfigurableTacticalAI(AIProfileDefinition profile, int? seed = null)
        {
            _profile = profile;
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public IAction DecideAction(ICombatState gameState, IUnit unit)
        {
            var scoredActions = EvaluateAllActions(gameState, unit);

            if (scoredActions.Count == 0)
            {
                return new EndUnitTurnAction(unit.Owner, unit.Id);
            }

            var bestAction = scoredActions.OrderByDescending(sa => sa.Score).First();
            Debug.Log($"[ConfigurableTacticalAI] Decided to perform {bestAction.Action.Type}.");
            return bestAction.Action;
        }

        private List<ScoredAction> EvaluateAllActions(ICombatState gameState, IUnit unit)
        {
            var scoredActions = new List<ScoredAction>();

            // Evaluate abilities with configurable weights
            var availableAbilities = unit.GetAvailableAbilities();
            foreach (var abilityInstance in availableAbilities)
            {
                var ability = abilityInstance.Ability;
                var validTargets = GetValidTargetsForAbility(gameState, unit, ability);

                foreach (var target in validTargets)
                {
                    var score = EvaluateAbilityAction(gameState, unit, ability, target);
                    scoredActions.Add(new ScoredAction(
                        new ScheduleAbilityAction(unit.Owner, unit.Id, ability.Id, target),
                        score
                    ));
                }
            }

            // Evaluate movement with configurable range
            var moveActions = EvaluateMovementActions(gameState, unit);
            scoredActions.AddRange(moveActions);

            // End turn action (low priority)
            scoredActions.Add(new ScoredAction(
                new EndUnitTurnAction(unit.Owner, unit.Id),
                10
            ));

            Debug.Log($"[ConfigurableTacticalAI] Identified {scoredActions.Count} valid actions.");
            return scoredActions;
        }

        private float EvaluateAbilityAction(
            ICombatState gameState,
            IUnit caster,
            IAbility ability,
            AbilityTarget target)
        {
            float score = 50;

            IUnit targetUnit = target.TargetUnitId.HasValue
                ? gameState.GetUnit(target.TargetUnitId.Value)
                : null;

            if (ability is IDamageAbility damageAbility && targetUnit != null)
            {
                // Use configurable damage weight
                score += damageAbility.Damage * _profile.DamageWeight;

                // Use configurable kill bonus threshold
                float hpPercent = (float)targetUnit.CurrentHP / targetUnit.MaxHP;
                if (hpPercent < _profile.KillThresholdPercent)
                {
                    score += _profile.KillBonus;
                }

                // Use configurable close range bonus
                int distance = CalculateDistance(caster.Position, targetUnit.Position);
                if (distance <= 1)
                {
                    score += _profile.CloseRangeBonus;
                }
            }

            if (ability is IHealAbility healAbility && targetUnit != null)
            {
                float hpPercent = (float)targetUnit.CurrentHP / targetUnit.MaxHP;

                if (hpPercent < _profile.DefensiveHpThreshold)
                {
                    // Scale healing priority based on how low HP is
                    score += (1 - hpPercent) * 100 * _profile.HealWeight;
                }
                else
                {
                    score -= 30; // Penalty for healing healthy allies
                }
            }

            if (ability is IStatusEffectAbility)
            {
                score += _profile.StatusEffectBonus;
            }

            return score;
        }

        private List<ScoredAction> EvaluateMovementActions(ICombatState gameState, IUnit unit)
        {
            var scoredActions = new List<ScoredAction>();

            // Use configurable movement range
            var validPositions = gameState.GetValidPositionsInRange(
                unit.Position,
                _profile.MovementRange);

            foreach (var position in validPositions)
            {
                var score = EvaluatePosition(gameState, unit, position);
                scoredActions.Add(new ScoredAction(
                    new MoveAction(unit.Owner, unit.Id, position),
                    score
                ));
            }

            return scoredActions;
        }

        private float EvaluatePosition(ICombatState gameState, IUnit unit, HexCoordinates position)
        {
            float score = 30;

            var enemies = gameState.Units.Where(u =>
                u.Owner.Id != unit.Owner.Id && u.IsAlive).ToList();

            if (enemies.Count > 0)
            {
                var closestEnemy = enemies.OrderBy(e =>
                    CalculateDistance(position, e.Position)).First();
                int distanceToEnemy = CalculateDistance(position, closestEnemy.Position);

                float hpPercent = (float)unit.CurrentHP / unit.MaxHP;

                if (hpPercent > _profile.DefensiveHpThreshold)
                {
                    // High HP: move closer (aggressive)
                    score += (10 - distanceToEnemy) * 5;
                }
                else
                {
                    // Low HP: keep distance (defensive)
                    score += distanceToEnemy * 3;
                }

                // Use configurable surround penalty
                int enemiesNearby = enemies.Count(e =>
                    CalculateDistance(position, e.Position) <= 2);
                score -= enemiesNearby * _profile.SurroundPenalty;
            }

            return score;
        }

        private List<AbilityTarget> GetValidTargetsForAbility(
            ICombatState gameState,
            IUnit caster,
            IAbility ability)
        {
            var targets = new List<AbilityTarget>();

            switch (ability.TargetType)
            {
                case AbilityTargetType.Self:
                    targets.Add(AbilityTarget.ForSelf());
                    break;

                case AbilityTargetType.Enemy:
                    foreach (var enemy in gameState.Units.Where(u =>
                        u.Owner.Id != caster.Owner.Id &&
                        u.IsAlive &&
                        CalculateDistance(caster.Position, u.Position) <= ability.Range))
                    {
                        targets.Add(AbilityTarget.ForUnit(enemy.Id, AbilityTargetType.Enemy));
                    }
                    break;

                case AbilityTargetType.Ally:
                    foreach (var ally in gameState.Units.Where(u =>
                        u.Owner.Id == caster.Owner.Id &&
                        u.IsAlive &&
                        u.Id != caster.Id &&
                        CalculateDistance(caster.Position, u.Position) <= ability.Range))
                    {
                        targets.Add(AbilityTarget.ForUnit(ally.Id, AbilityTargetType.Ally));
                    }
                    break;
            }

            return targets;
        }

        private int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = System.Math.Abs(from.Q - to.Q);
            var dr = System.Math.Abs(from.R - to.R);
            var ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));
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
