using System;
using System.Collections.Generic;
using Inventory.Core;

namespace Mutation.Core
{
    /// <summary>
    /// Pure socketing state over the inventory, the blank rack, and the authored
    /// blank data (socket counts). Arranging stays free even on a ready (full)
    /// blank — the commit point is the player's unseal confirm (Track F medallion
    /// beat), which calls ConsumeSockets; ReturnAll dismisses the whole table.
    /// </summary>
    public class SocketingModel : ISocketingModel
    {
        private static readonly IReadOnlyList<ArtifactInstance> NoArtifacts = Array.Empty<ArtifactInstance>();

        private readonly IInventoryModel _inventory;
        private readonly IBlankRack _rack;
        private readonly IPartBlankDataSource _blankData;
        private readonly Dictionary<int, List<ArtifactInstance>> _socketsByBlank =
            new Dictionary<int, List<ArtifactInstance>>();

        public event Action<int> OnSocketsChanged;
        public event Action<int> OnBlankReady;
        public event Action<IReadOnlyList<ArtifactInstance>> OnSocketsConsumed;

        public SocketingModel(IInventoryModel inventory, IBlankRack rack, IPartBlankDataSource blankData)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _rack = rack ?? throw new ArgumentNullException(nameof(rack));
            _blankData = blankData ?? throw new ArgumentNullException(nameof(blankData));
        }

        public IReadOnlyList<ArtifactInstance> SocketedArtifacts(int blankInstanceId)
        {
            return _socketsByBlank.TryGetValue(blankInstanceId, out var sockets)
                ? sockets
                : NoArtifacts;
        }

        public bool IsReady(int blankInstanceId)
        {
            return TryGetSocketCount(blankInstanceId, out int socketCount)
                   && SocketedArtifacts(blankInstanceId).Count >= socketCount;
        }

        public bool TrySocket(int blankInstanceId, int artifactInstanceId)
        {
            if (!TryGetSocketCount(blankInstanceId, out int socketCount))
            {
                return false;
            }

            if (!_socketsByBlank.TryGetValue(blankInstanceId, out var sockets))
            {
                sockets = new List<ArtifactInstance>(socketCount);
                _socketsByBlank[blankInstanceId] = sockets;
            }

            if (sockets.Count >= socketCount)
            {
                return false;
            }

            if (!_inventory.TryGet(artifactInstanceId, out var artifact))
            {
                return false;
            }

            _inventory.Remove(artifactInstanceId);
            sockets.Add(artifact);
            OnSocketsChanged?.Invoke(blankInstanceId);

            if (sockets.Count >= socketCount)
            {
                OnBlankReady?.Invoke(blankInstanceId);
            }

            return true;
        }

        public bool TryUnsocket(int blankInstanceId, int artifactInstanceId)
        {
            if (!_socketsByBlank.TryGetValue(blankInstanceId, out var sockets))
            {
                return false;
            }

            // Re-slotting stays free until the unseal confirm (the commit moved
            // from "last drop" to the medallion's confirm beat): unsocketing a
            // ready blank simply reopens it.
            for (int i = 0; i < sockets.Count; i++)
            {
                if (sockets[i].InstanceId == artifactInstanceId)
                {
                    var artifact = sockets[i];
                    sockets.RemoveAt(i);
                    _inventory.Return(artifact);
                    OnSocketsChanged?.Invoke(blankInstanceId);
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<ArtifactInstance> ConsumeSockets(int blankInstanceId)
        {
            if (!_socketsByBlank.TryGetValue(blankInstanceId, out var sockets) || sockets.Count == 0)
            {
                return NoArtifacts;
            }

            var consumed = new List<ArtifactInstance>(sockets);
            sockets.Clear();
            _socketsByBlank.Remove(blankInstanceId);
            OnSocketsChanged?.Invoke(blankInstanceId);
            OnSocketsConsumed?.Invoke(consumed);
            return consumed;
        }

        public void ReturnAll()
        {
            if (_socketsByBlank.Count == 0)
            {
                return;
            }

            var blankIds = new List<int>(_socketsByBlank.Keys);
            foreach (var blankId in blankIds)
            {
                var sockets = _socketsByBlank[blankId];
                foreach (var artifact in sockets)
                {
                    _inventory.Return(artifact);
                }

                sockets.Clear();
                _socketsByBlank.Remove(blankId);
                OnSocketsChanged?.Invoke(blankId);
            }
        }

        private bool TryGetSocketCount(int blankInstanceId, out int socketCount)
        {
            socketCount = 0;

            if (!_rack.TryGet(blankInstanceId, out var instance))
            {
                return false;
            }

            if (!_blankData.TryGet(instance.DefinitionId, out var blank))
            {
                return false;
            }

            socketCount = blank.SocketCount;
            return true;
        }
    }
}
