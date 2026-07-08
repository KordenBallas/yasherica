using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;

namespace Combat.Player.AI
{
    /// <summary>
    /// Scores a candidate set from actually-simulated outcomes (via the same
    /// IAbilityOutcomeCalculator the ghost telegraph uses, so AI expectations match
    /// execution): damage/kills on hostiles score up, friendly fire scores down, heals are
    /// worth only the HP they restore. A move destination earns a small positioning term
    /// plus a discounted lookahead of how much it IMPROVES the unit's best shot over its
    /// current position — improvement, not absolute value, so a unit that can already hit
    /// attacks instead of endlessly repositioning.
    /// </summary>
    public sealed class AIActionScorer
    {
        // Positioning is deliberately an order of magnitude below damage terms: it breaks
        // ties and creates approach/retreat gradients, never outbids a real hit.
        private const float ApproachScorePerCell = 2f;
        private const float RetreatScorePerCell = 3f;
        private const int ApproachReferenceDistance = 10;
        private const int SurroundRadius = 2;
        private const int AdjacentDistance = 1;

        private readonly IAbilityOutcomeCalculator _outcomes;
        private readonly IHostilityPolicy _hostility;

        public AIActionScorer(IAbilityOutcomeCalculator outcomes, IHostilityPolicy hostility)
        {
            _outcomes = outcomes ?? throw new ArgumentNullException(nameof(outcomes));
            _hostility = hostility ?? throw new ArgumentNullException(nameof(hostility));
        }

        public IReadOnlyList<AIScoredCandidate> ScoreAll(
            ICombatState state, IUnit unit, IReadOnlyList<AICandidate> candidates, AITuning tuning)
        {
            // The lookahead baseline: the best the unit could do without moving. Computed
            // once per decision, not per move candidate.
            float currentBest = BestAbilityScoreFrom(
                state, unit, unit.Position, tuning, excludeCaster: false);

            var scored = new List<AIScoredCandidate>(candidates.Count);
            foreach (var candidate in candidates)
                scored.Add(new AIScoredCandidate(
                    candidate, Score(state, unit, candidate, tuning, currentBest)));
            return scored;
        }

        private float Score(
            ICombatState state, IUnit unit, AICandidate candidate, AITuning tuning, float currentBest)
        {
            switch (candidate.Kind)
            {
                case AICandidateKind.Ability:
                    return ScoreAbility(state, unit, candidate.Ability, candidate.Facing,
                        unit.Position, tuning, excludeCaster: false);
                case AICandidateKind.Move:
                    return ScoreMove(state, unit, candidate.MoveDestination, tuning, currentBest);
                default:
                    return AITuning.EndTurnBaselineScore;
            }
        }

        private float ScoreAbility(
            ICombatState state,
            IUnit unit,
            IAbility ability,
            HexDirection? facing,
            HexCoordinates origin,
            AITuning tuning,
            bool excludeCaster)
        {
            // Ring shapes ignore the direction argument; any fixed value keeps this deterministic.
            var outcome = _outcomes.ComputeForFacing(
                state, unit, ability, facing ?? HexDirection.E, origin);

            float score = 0f;

            foreach (var unitOutcome in outcome.Units)
            {
                if (excludeCaster && unitOutcome.UnitId == unit.Id)
                    continue;

                var target = state.GetUnit(unitOutcome.UnitId);
                if (target == null || !target.IsAlive)
                    continue;

                bool hostile = _hostility.AreHostile(unit, target);
                bool predictedKill = target.CurrentHP - unitOutcome.Damage + unitOutcome.Heal <= 0;

                if (unitOutcome.Damage > 0)
                {
                    if (hostile)
                    {
                        score += unitOutcome.Damage * tuning.DamageWeight * tuning.AggressionWeight;
                        if (predictedKill)
                            score += tuning.KillBonus * tuning.AggressionWeight;
                        score += tuning.FocusWoundedWeight * (1f - (float)target.CurrentHP / target.MaxHP);
                    }
                    else
                    {
                        score -= unitOutcome.Damage * tuning.FriendlyFirePenaltyWeight;
                        if (predictedKill)
                            score -= tuning.KillBonus * tuning.FriendlyFirePenaltyWeight;
                    }
                }

                if (unitOutcome.Heal > 0 && !hostile)
                {
                    int missingHp = target.MaxHP - target.CurrentHP;
                    score += Math.Min(unitOutcome.Heal, missingHp) * tuning.HealWeight;
                }

                if (ability is IStatusEffectAbility statusAbility && statusAbility.EffectToApply != null)
                {
                    if (hostile)
                    {
                        bool alreadyCarrying = target.StatusEffects
                            .Any(effect => effect.Id == statusAbility.EffectToApply.Id);
                        if (!alreadyCarrying)
                            score += tuning.StatusEffectBonus;
                    }
                    else
                    {
                        score -= tuning.StatusEffectBonus;
                    }
                }
            }

            return score;
        }

        private float ScoreMove(
            ICombatState state, IUnit unit, HexCoordinates destination, AITuning tuning, float currentBest)
        {
            float score = ScorePositioning(state, unit, destination, tuning);

            float destinationBest = BestAbilityScoreFrom(
                state, unit, destination, tuning, excludeCaster: true);
            score += Math.Max(0f, destinationBest - currentBest) * AITuning.LookaheadDiscount;

            return score;
        }

        private float ScorePositioning(ICombatState state, IUnit unit, HexCoordinates position, AITuning tuning)
        {
            var hostiles = state.Units
                .Where(other => other.IsAlive && _hostility.AreHostile(unit, other))
                .ToList();
            if (hostiles.Count == 0)
                return 0f;

            int nearestDistance = hostiles.Min(h => state.CalculateDistance(position, h.Position));
            float hpFraction = (float)unit.CurrentHP / unit.MaxHP;
            float score = 0f;

            if (hpFraction > tuning.DefensiveHpThreshold)
            {
                score += (ApproachReferenceDistance - nearestDistance) * ApproachScorePerCell;
                if (nearestDistance <= AdjacentDistance)
                    score += tuning.CloseRangeBonus;
            }
            else
            {
                score += nearestDistance * RetreatScorePerCell * tuning.SelfPreservationWeight;
            }

            int hostilesNearby = hostiles.Count(
                h => state.CalculateDistance(position, h.Position) <= SurroundRadius);
            score -= hostilesNearby * tuning.SurroundPenalty;

            return score;
        }

        /// <summary>
        /// Best single-ability score achievable from the given origin. For hypothetical
        /// origins the caster is excluded from the simulated outcome because it still
        /// occupies its old cell in the state.
        /// </summary>
        private float BestAbilityScoreFrom(
            ICombatState state, IUnit unit, HexCoordinates origin, AITuning tuning, bool excludeCaster)
        {
            float best = 0f;

            foreach (var abilityInstance in unit.GetAvailableAbilities())
            {
                var ability = abilityInstance.Ability;
                foreach (var facing in AIFacings.For(ability))
                {
                    float score = ScoreAbility(
                        state, unit, ability, facing, origin, tuning, excludeCaster);
                    if (score > best)
                        best = score;
                }
            }

            return best;
        }
    }
}
