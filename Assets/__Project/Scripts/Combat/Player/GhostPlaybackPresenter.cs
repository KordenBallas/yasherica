using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Execution;
using Core.Logging;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Drives the ghost telegraph: plays a one-shot full-outcome ghost when the player
    /// queues an ability (detected as queue growth, so any scheduling path triggers it),
    /// and replays a queued ability's or an enemy intent's ghost on icon hover. Outcomes
    /// are always computed against the CURRENT board — the ghost never simulates the queue.
    /// </summary>
    public class GhostPlaybackPresenter : IDisposable
    {
        private readonly ICombatController _combatController;
        private readonly IAbilityOutcomeCalculator _outcomeCalculator;
        private readonly HexDirectionConfig _hexConfig;
        private readonly Func<HexCoordinates, Vector3> _hexToWorld;
        private readonly IGhostPlaybackView _view;
        private readonly IGameLogger _logger;

        private readonly Dictionary<int, int> _lastQueueCounts = new Dictionary<int, int>();

        public GhostPlaybackPresenter(
            ICombatController combatController,
            IAbilityOutcomeCalculator outcomeCalculator,
            HexDirectionConfig hexConfig,
            Func<HexCoordinates, Vector3> hexToWorld,
            IGhostPlaybackView view,
            IGameLogger logger)
        {
            _combatController = combatController;
            _outcomeCalculator = outcomeCalculator;
            _hexConfig = hexConfig;
            _hexToWorld = hexToWorld;
            _view = view;
            _logger = logger;

            _combatController.OnStateChanged += HandleStateChanged;
        }

        public void Dispose()
        {
            _combatController.OnStateChanged -= HandleStateChanged;
            _view.Stop();
        }

        /// <summary>
        /// Replays the ghost of a queued player ability (icon hover), against the current board.
        /// </summary>
        public void PlayForQueueIndex(int unitId, int queueIndex)
        {
            var state = _combatController.CombatState;
            var unit = state?.GetUnit(unitId);
            if (unit == null) return;

            var scheduled = unit.AbilityQueue.FirstOrDefault(s => s.ExecutionOrder == queueIndex);
            if (scheduled.Ability == null)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[GhostPlaybackPresenter] No queued ability at order {queueIndex} on unit {unitId}");
                return;
            }

            PlayOutcome(state, unit, scheduled.Ability.Ability, unit.FacingDirection);
        }

        /// <summary>
        /// Replays the ghost of an enemy's committed intent (icon hover) — reading the enemy.
        /// </summary>
        public void PlayForEnemyIntent(int unitId)
        {
            var state = _combatController.CombatState;
            var intent = state?.EnemyIntents.FirstOrDefault(i => i.UnitId == unitId);
            var caster = state?.GetUnit(unitId);
            if (intent == null || caster == null || !intent.IsAbility) return;

            var outcome = _outcomeCalculator.ComputeCommitted(state, intent);
            _view.Play(GhostPlaybackPlanBuilder.Build(
                outcome, caster, intent.CommittedFacing, _hexConfig, _hexToWorld));
        }

        /// <summary>
        /// Stops the running ghost (icon hover ended).
        /// </summary>
        public void Stop()
        {
            _view.Stop();
        }

        /// <summary>
        /// Queue growth on a human unit = an ability was just submitted → auto-play its ghost once.
        /// </summary>
        private void HandleStateChanged(ICombatState state)
        {
            if (state == null) return;

            foreach (var unit in state.Units)
            {
                if (unit.Owner.Type != PlayerType.Human)
                    continue;

                int count = unit.AbilityQueue.Count;
                _lastQueueCounts.TryGetValue(unit.Id, out int lastCount);
                _lastQueueCounts[unit.Id] = count;

                if (count > lastCount && unit.IsAlive)
                {
                    var newest = unit.AbilityQueue.OrderBy(s => s.ExecutionOrder).Last();
                    PlayOutcome(state, unit, newest.Ability.Ability, unit.FacingDirection);
                }
            }
        }

        private void PlayOutcome(ICombatState state, IUnit caster, IAbility ability, HexDirection facing)
        {
            var outcome = _outcomeCalculator.ComputeForFacing(state, caster, ability, facing);
            var lineDirection = ability.Shape.Type == AbilityShapeType.Line
                ? facing
                : (HexDirection?)null;

            _view.Play(GhostPlaybackPlanBuilder.Build(
                outcome, caster, lineDirection, _hexConfig, _hexToWorld));
        }
    }
}
