using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// The full, deterministic outcome of an install request as computed by
    /// <see cref="BodyPlanChangePlanner"/>: what kind of change it is, which skeleton governs
    /// afterwards, and — for a frame change — how every part of the prospective body is
    /// classified (active on the new frame / equipped-but-dormant / shed to inventory).
    /// All lists are sorted by slot id (ordinal) so the same equipped set always yields an
    /// identical plan (FR11) and the confirm dialog lists shed parts in a stable order.
    /// </summary>
    public sealed class BodyPlanChangePlan
    {
        public BodyPlanChangeKind Kind { get; }
        public string GoverningSkeletonId { get; }

        /// <summary>Winning frame-changer part id; null when the base plan governs.</summary>
        public string GoverningPartId { get; }

        /// <summary>Parts rendered on the governing frame after the change (frame change only;
        /// includes the incoming part). Empty for the other kinds.</summary>
        public IReadOnlyList<PartData> ActiveParts { get; }

        /// <summary>Losing frame-changers kept equipped-but-dormant (frame change only).</summary>
        public IReadOnlyList<PartData> DormantParts { get; }

        /// <summary>Ordinary parts with no home on the governing frame — returned to the
        /// player's inventory (frame change only).</summary>
        public IReadOnlyList<PartData> ShedParts { get; }

        /// <summary>A frame change that sheds parts must be confirmed by the player first
        /// (FR7); a shed-nothing change needs no prompt.</summary>
        public bool RequiresConfirmation => Kind == BodyPlanChangeKind.FrameChange && ShedParts.Count > 0;

        public BodyPlanChangePlan(
            BodyPlanChangeKind kind,
            string governingSkeletonId,
            string governingPartId,
            IReadOnlyList<PartData> activeParts = null,
            IReadOnlyList<PartData> dormantParts = null,
            IReadOnlyList<PartData> shedParts = null)
        {
            Kind = kind;
            GoverningSkeletonId = governingSkeletonId;
            GoverningPartId = governingPartId;
            ActiveParts = activeParts ?? Array.Empty<PartData>();
            DormantParts = dormantParts ?? Array.Empty<PartData>();
            ShedParts = shedParts ?? Array.Empty<PartData>();
        }
    }
}
