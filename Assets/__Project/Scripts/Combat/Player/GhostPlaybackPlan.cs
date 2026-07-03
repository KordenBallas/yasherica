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

        public GhostPlaybackPlan(
            int casterUnitId,
            Vector3 casterPosition,
            Vector3 casterLookDirection,
            IReadOnlyList<GhostUnitMarker> markers)
        {
            CasterUnitId = casterUnitId;
            CasterPosition = casterPosition;
            CasterLookDirection = casterLookDirection;
            Markers = markers ?? new List<GhostUnitMarker>();
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
