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

        public PartData(
            string partId,
            string slotId,
            string targetSkeletonId,
            IReadOnlyList<string> boneNames,
            IReadOnlyList<SocketInfo> contributedSockets)
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
        }
    }
}
