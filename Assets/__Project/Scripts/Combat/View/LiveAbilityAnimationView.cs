using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Core;
using Combat.Execution;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Plays the live (opaque) placeholder ability animation (D3): subscribes to the shared
    /// <see cref="IAbilityFiredSink"/> and, whenever an ability executes — the player's queue or an
    /// enemy's committed intent — sweeps <see cref="AbilityAreaSweep"/> across the struck cells. The
    /// enemy path is naturally paced by <c>EnemyRoundController</c>'s resolve beat, so the enemy action
    /// reads as visibly-not-instant. Purely visible playback downstream of the state change — it never
    /// touches outcomes. Thin adapter; one per combat, created/disposed by <c>CombatActiveState</c>.
    /// </summary>
    public sealed class LiveAbilityAnimationView : MonoBehaviour
    {
        private static readonly Color StrikeTint = new Color(1f, 0.55f, 0.30f);
        private static readonly Color HealTint = new Color(0.45f, 1f, 0.55f);

        private IAbilityFiredSink _sink;
        private ICombatUnitViewRegistry _registry;
        private Func<HexCoordinates, Vector3> _hexToWorld;

        public void Initialize(IAbilityFiredSink sink, ICombatUnitViewRegistry registry,
            Func<HexCoordinates, Vector3> hexToWorld)
        {
            _sink = sink;
            _registry = registry;
            _hexToWorld = hexToWorld;

            if (_sink != null)
                _sink.Fired += HandleFired;
        }

        private void OnDestroy()
        {
            if (_sink != null)
                _sink.Fired -= HandleFired;
        }

        private void HandleFired(AbilityFiredCue cue)
        {
            if (_hexToWorld == null || cue.Cells == null || cue.Cells.Count == 0)
                return;

            var worldCells = cue.Cells.Select(c => _hexToWorld(c)).ToList();
            var sweepOrigin = CasterWorldPosition(cue.CasterUnitId, worldCells);
            var tint = cue.IsHeal ? HealTint : StrikeTint;

            AbilityAreaSweep.Play(
                transform,
                worldCells,
                sweepOrigin,
                isLine: cue.Shape == AbilityShapeType.Line,
                tint,
                TelegraphStyle.AbilityFlashLiveAlpha,
                TelegraphStyle.AbilitySweepSeconds,
                TelegraphStyle.AbilityFlashCellSize);
        }

        /// <summary>The caster's world position (for the line-sweep origin); falls back to the first cell.</summary>
        private Vector3 CasterWorldPosition(int casterUnitId, List<Vector3> worldCells)
        {
            if (_registry != null && _registry.TryGet(casterUnitId, out var visual) && visual != null)
                return visual.position;

            return worldCells[0];
        }
    }
}
