using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Default <see cref="IFeedingSession"/>: a simple tray that pulls artifacts out of the
    /// inventory on select (so their bubble disappears, like crafting staging) and either returns
    /// them on unselect/close or hands them off on Consume for the presenter to digest.
    /// </summary>
    public sealed class FeedingSession : IFeedingSession
    {
        private readonly IInventoryModel _inventory;
        private readonly List<ArtifactInstance> _tray = new List<ArtifactInstance>();

        public FeedingSession(IInventoryModel inventory)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public IReadOnlyList<ArtifactInstance> Tray => _tray;

        public event Action OnTrayChanged;

        public bool TrySelect(int instanceId)
        {
            if (!_inventory.TryGet(instanceId, out var instance))
            {
                return false;
            }

            _inventory.Remove(instanceId);
            _tray.Add(instance);
            OnTrayChanged?.Invoke();
            return true;
        }

        public bool TryUnselect(int instanceId)
        {
            int index = IndexOf(instanceId);
            if (index < 0)
            {
                return false;
            }

            var item = _tray[index];
            _tray.RemoveAt(index);
            _inventory.Return(item);
            OnTrayChanged?.Invoke();
            return true;
        }

        public IReadOnlyList<ArtifactInstance> Consume()
        {
            if (_tray.Count == 0)
            {
                return Array.Empty<ArtifactInstance>();
            }

            var consumed = new List<ArtifactInstance>(_tray);
            _tray.Clear();
            OnTrayChanged?.Invoke();
            return consumed;
        }

        public void ReturnAll()
        {
            if (_tray.Count == 0)
            {
                return;
            }

            var returned = new List<ArtifactInstance>(_tray);
            _tray.Clear();

            foreach (var instance in returned)
            {
                _inventory.Return(instance);
            }

            OnTrayChanged?.Invoke();
        }

        private int IndexOf(int instanceId)
        {
            for (int i = 0; i < _tray.Count; i++)
            {
                if (_tray[i].InstanceId == instanceId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
