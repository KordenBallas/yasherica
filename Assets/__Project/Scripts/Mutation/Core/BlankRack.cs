using System;
using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Capped Part-Blank container with its own instance-id generation
    /// (blank ids and artifact ids are separate id spaces; a blank never
    /// enters the artifact inventory).
    /// </summary>
    public class BlankRack : IBlankRack
    {
        private readonly List<BlankInstance> _blanks = new List<BlankInstance>();
        private int _nextInstanceId = 1;

        public int Capacity { get; }

        public IReadOnlyList<BlankInstance> Blanks => _blanks;

        public event Action OnChanged;

        public BlankRack(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), $"Blank rack capacity must be at least 1, got {capacity}.");
            }

            Capacity = capacity;
        }

        public bool TryAdd(string definitionId, out BlankInstance instance)
        {
            instance = null;

            if (string.IsNullOrEmpty(definitionId) || _blanks.Count >= Capacity)
            {
                return false;
            }

            instance = new BlankInstance(_nextInstanceId++, definitionId);
            _blanks.Add(instance);
            OnChanged?.Invoke();
            return true;
        }

        public bool Remove(int instanceId)
        {
            for (int i = 0; i < _blanks.Count; i++)
            {
                if (_blanks[i].InstanceId == instanceId)
                {
                    _blanks.RemoveAt(i);
                    OnChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public bool TryGet(int instanceId, out BlankInstance instance)
        {
            foreach (var blank in _blanks)
            {
                if (blank.InstanceId == instanceId)
                {
                    instance = blank;
                    return true;
                }
            }

            instance = null;
            return false;
        }
    }
}
