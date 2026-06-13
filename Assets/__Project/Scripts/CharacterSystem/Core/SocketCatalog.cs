using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Maintains the queryable union of Tier-1 (skeleton) sockets and Tier-2 sockets
    /// contributed by the currently equipped parts. Rebuilt on every part change;
    /// raises a diff event so the Unity layer can create/destroy socket transforms
    /// without owning any diff logic itself.
    /// </summary>
    public sealed class SocketCatalog
    {
        // Insertion-ordered: Tier-1 first, then parts in the order they were passed
        // to Rebuild. On duplicate ids the first occurrence wins deterministically.
        private readonly Dictionary<string, SocketInfo> _socketsById = new Dictionary<string, SocketInfo>(StringComparer.Ordinal);
        private readonly List<SocketInfo> _orderedSockets = new List<SocketInfo>();

        public event Action<IReadOnlyList<SocketInfo>, IReadOnlyList<SocketInfo>> Changed;

        public IReadOnlyList<SocketInfo> AvailableSockets => _orderedSockets;

        public bool TryGet(string socketId, out SocketInfo socket)
        {
            if (socketId == null)
            {
                socket = default;
                return false;
            }

            return _socketsById.TryGetValue(socketId, out socket);
        }

        public bool Contains(string socketId)
        {
            return socketId != null && _socketsById.ContainsKey(socketId);
        }

        /// <summary>
        /// Recomputes the union from the skeleton and the equipped parts, then raises
        /// <see cref="Changed"/> with the exact added/removed diff. A socket whose id
        /// survives but whose definition changed (e.g. contributed by a different part)
        /// appears in both lists: removed (old) and added (new).
        /// </summary>
        public void Rebuild(SkeletonData skeleton, IEnumerable<PartData> equippedParts)
        {
            if (skeleton == null)
            {
                throw new ArgumentNullException(nameof(skeleton));
            }

            var newById = new Dictionary<string, SocketInfo>(StringComparer.Ordinal);
            var newOrdered = new List<SocketInfo>();

            foreach (var socket in skeleton.Tier1Sockets)
            {
                AddIfNew(newById, newOrdered, socket);
            }

            if (equippedParts != null)
            {
                foreach (var part in equippedParts)
                {
                    foreach (var socket in part.ContributedSockets)
                    {
                        AddIfNew(newById, newOrdered, socket);
                    }
                }
            }

            var removed = new List<SocketInfo>();
            var added = new List<SocketInfo>();

            foreach (var old in _orderedSockets)
            {
                if (!newById.TryGetValue(old.Id, out var replacement) || !replacement.Equals(old))
                {
                    removed.Add(old);
                }
            }

            foreach (var fresh in newOrdered)
            {
                if (!_socketsById.TryGetValue(fresh.Id, out var previous) || !previous.Equals(fresh))
                {
                    added.Add(fresh);
                }
            }

            _socketsById.Clear();
            _orderedSockets.Clear();
            foreach (var socket in newOrdered)
            {
                _socketsById.Add(socket.Id, socket);
                _orderedSockets.Add(socket);
            }

            if (removed.Count > 0 || added.Count > 0)
            {
                Changed?.Invoke(added, removed);
            }
        }

        private static void AddIfNew(
            Dictionary<string, SocketInfo> byId,
            List<SocketInfo> ordered,
            SocketInfo socket)
        {
            if (byId.ContainsKey(socket.Id))
            {
                // Duplicate ids are a validation issue surfaced by AssemblyValidator;
                // the catalog stays deterministic by keeping the first occurrence.
                return;
            }

            byId.Add(socket.Id, socket);
            ordered.Add(socket);
        }
    }
}
