using System;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Immutable description of one socket: where it lives and who provides it.
    /// Equality covers all fields so the catalog diff treats a socket whose
    /// contributing part changed as removed + re-added (the mounter relies on this
    /// to recreate the transform and re-parent attachments).
    /// </summary>
    public readonly struct SocketInfo : IEquatable<SocketInfo>
    {
        public string Id { get; }
        public string ParentBoneName { get; }
        public SocketTier Tier { get; }

        /// <summary>Null for skeleton (Tier-1) sockets.</summary>
        public string SourcePartId { get; }

        public SocketInfo(string id, string parentBoneName, SocketTier tier, string sourcePartId = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Socket id must be a non-empty string.", nameof(id));
            }

            Id = id;
            ParentBoneName = parentBoneName;
            Tier = tier;
            SourcePartId = sourcePartId;
        }

        public bool Equals(SocketInfo other)
        {
            return Id == other.Id
                   && ParentBoneName == other.ParentBoneName
                   && Tier == other.Tier
                   && SourcePartId == other.SourcePartId;
        }

        public override bool Equals(object obj)
        {
            return obj is SocketInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Id != null ? Id.GetHashCode() : 0;
                hash = (hash * 397) ^ (ParentBoneName != null ? ParentBoneName.GetHashCode() : 0);
                hash = (hash * 397) ^ (int)Tier;
                hash = (hash * 397) ^ (SourcePartId != null ? SourcePartId.GetHashCode() : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return SourcePartId == null
                ? $"{Id} (skeleton, bone: {ParentBoneName})"
                : $"{Id} (part: {SourcePartId}, bone: {ParentBoneName})";
        }
    }
}
