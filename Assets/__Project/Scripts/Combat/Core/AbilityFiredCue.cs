using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;

namespace Combat.Core
{
    /// <summary>
    /// A presentation cue emitted when an ability actually executes (D3): who cast it, its shape, the
    /// cells it struck, and the line direction. Read-only, emitted after the state has already mutated —
    /// it drives the live ability animation only; it never changes outcomes, cells, or timing. Pure
    /// domain types so the executor that emits it stays UnityEngine-free.
    /// </summary>
    public readonly struct AbilityFiredCue
    {
        public int CasterUnitId { get; }
        public AbilityShapeType Shape { get; }
        public IReadOnlyList<HexCoordinates> Cells { get; }
        public HexDirection? LineDirection { get; }

        /// <summary>True for a healing ability, so the animation can tint supportively rather than as a strike.</summary>
        public bool IsHeal { get; }

        public AbilityFiredCue(int casterUnitId, AbilityShapeType shape,
            IReadOnlyList<HexCoordinates> cells, HexDirection? lineDirection, bool isHeal)
        {
            CasterUnitId = casterUnitId;
            Shape = shape;
            Cells = cells ?? new List<HexCoordinates>();
            LineDirection = lineDirection;
            IsHeal = isHeal;
        }
    }
}
