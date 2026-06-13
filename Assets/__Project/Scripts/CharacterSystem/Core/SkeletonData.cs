using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Pure-C# snapshot of a skeleton: its identity, bone names, and Tier-1 sockets.
    /// Mapped from SkeletonDefinition so the domain never touches ScriptableObjects.
    /// </summary>
    public sealed class SkeletonData
    {
        public string SkeletonId { get; }
        public IReadOnlyList<string> BoneNames { get; }
        public IReadOnlyList<SocketInfo> Tier1Sockets { get; }

        private readonly HashSet<string> _boneNameSet;

        public SkeletonData(string skeletonId, IReadOnlyList<string> boneNames, IReadOnlyList<SocketInfo> tier1Sockets)
        {
            if (string.IsNullOrEmpty(skeletonId))
            {
                throw new ArgumentException("Skeleton id must be a non-empty string.", nameof(skeletonId));
            }

            SkeletonId = skeletonId;
            BoneNames = boneNames ?? Array.Empty<string>();
            Tier1Sockets = tier1Sockets ?? Array.Empty<SocketInfo>();
            _boneNameSet = new HashSet<string>(BoneNames, StringComparer.Ordinal);
        }

        public bool HasBone(string boneName)
        {
            return boneName != null && _boneNameSet.Contains(boneName);
        }
    }
}
