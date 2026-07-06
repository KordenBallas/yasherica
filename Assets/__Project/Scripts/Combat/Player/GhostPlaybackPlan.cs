using System.Collections.Generic;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Everything the ghost view needs to play one ability preview: where the caster ghost
    /// stands and looks, and a marker per affected unit (predicted destination + numbers).
    /// Pure data — built by GhostPlaybackPlanBuilder from an AbilityOutcome.
    /// </summary>
    public class GhostPlaybackPlan
    {
        public int CasterUnitId { get; }
        public Vector3 CasterPosition { get; }

        /// <summary>
        /// World direction of the volley; zero for ring abilities (keep current rotation).
        /// </summary>
        public Vector3 CasterLookDirection { get; }

        public IReadOnlyList<GhostUnitMarker> Markers { get; }

        /// <summary>
        /// World positions of every cell the ability affects (D3) — the translucent cell-sweep animation
        /// plays across these, so the ghost preview now moves. <see cref="IsLine"/> chooses the sweep vs
        /// simultaneous ring pop; <see cref="SweepOrigin"/> (the caster's world position) orders the sweep.
        /// </summary>
        public IReadOnlyList<Vector3> AffectedCellPositions { get; }
        public Vector3 SweepOrigin { get; }
        public bool IsLine { get; }

        public GhostPlaybackPlan(
            int casterUnitId,
            Vector3 casterPosition,
            Vector3 casterLookDirection,
            IReadOnlyList<GhostUnitMarker> markers,
            IReadOnlyList<Vector3> affectedCellPositions = null,
            Vector3 sweepOrigin = default,
            bool isLine = false)
        {
            CasterUnitId = casterUnitId;
            CasterPosition = casterPosition;
            CasterLookDirection = casterLookDirection;
            Markers = markers ?? new List<GhostUnitMarker>();
            AffectedCellPositions = affectedCellPositions ?? new List<Vector3>();
            SweepOrigin = sweepOrigin;
            IsLine = isLine;
        }
    }

    /// <summary>
    /// Ghost marker for one affected unit: predicted displacement (From→To in world space)
    /// and the damage/heal numbers to label it with.
    /// </summary>
    public readonly struct GhostUnitMarker
    {
        public int UnitId { get; }
        public Vector3 From { get; }
        public Vector3 To { get; }
        public int Damage { get; }
        public int Heal { get; }
        public bool IsDisplaced { get; }

        public GhostUnitMarker(int unitId, Vector3 from, Vector3 to, int damage, int heal, bool isDisplaced)
        {
            UnitId = unitId;
            From = from;
            To = to;
            Damage = damage;
            Heal = heal;
            IsDisplaced = isDisplaced;
        }
    }
}
