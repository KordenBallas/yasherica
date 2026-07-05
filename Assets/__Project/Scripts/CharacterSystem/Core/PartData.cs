using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Pure-C# snapshot of a body part: which slot it fills, which skeleton it targets,
    /// the bone names its mesh is skinned to (ordered to match the mesh bone indices),
    /// and the Tier-2 sockets it contributes while equipped.
    /// </summary>
    public sealed class PartData
    {
        public string PartId { get; }
        public string SlotId { get; }
        public string TargetSkeletonId { get; }

        /// <summary>Ordered to match the part mesh's bone indices / bindposes.</summary>
        public IReadOnlyList<string> BoneNames { get; }

        public IReadOnlyList<SocketInfo> ContributedSockets { get; }

        /// <summary>True for the rare frame-changing parts: while equipped, this part is a
        /// candidate to govern the whole body plan (its <see cref="TargetSkeletonId"/> becomes
        /// the body's skeleton when it wins the priority resolution).</summary>
        public bool GovernsBodyPlan { get; }

        /// <summary>Authored priority among equipped frame-changing parts; the highest wins,
        /// ties break by ordinal <see cref="PartId"/>. Meaningless when
        /// <see cref="GovernsBodyPlan"/> is false.</summary>
        public int BodyPlanPriority { get; }

        public PartData(
            string partId,
            string slotId,
            string targetSkeletonId,
            IReadOnlyList<string> boneNames,
            IReadOnlyList<SocketInfo> contributedSockets,
            bool governsBodyPlan = false,
            int bodyPlanPriority = 0)
        {
            if (string.IsNullOrEmpty(partId))
            {
                throw new ArgumentException("Part id must be a non-empty string.", nameof(partId));
            }

            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("Slot id must be a non-empty string.", nameof(slotId));
            }

            PartId = partId;
            SlotId = slotId;
            TargetSkeletonId = targetSkeletonId;
            BoneNames = boneNames ?? Array.Empty<string>();
            ContributedSockets = contributedSockets ?? Array.Empty<SocketInfo>();
            GovernsBodyPlan = governsBodyPlan;
            BodyPlanPriority = bodyPlanPriority;
        }
    }
}
