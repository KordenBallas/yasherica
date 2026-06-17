using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Tracks which part currently occupies which slot. Slots are open/data-driven:
    /// equipping into an unknown slot id simply creates the slot entry.
    /// </summary>
    public sealed class CharacterAssemblyState
    {
        private readonly Dictionary<string, PartData> _equippedBySlot = new Dictionary<string, PartData>(StringComparer.Ordinal);

        public event Action<PartData> PartEquipped;
        public event Action<PartData> PartRemoved;

        public IReadOnlyCollection<PartData> EquippedParts => _equippedBySlot.Values;

        /// <summary>Snapshot of currently equipped parts as a slotId -&gt; partId map.</summary>
        public IReadOnlyDictionary<string, string> EquippedPartIdsBySlot()
        {
            var map = new Dictionary<string, string>(_equippedBySlot.Count, StringComparer.Ordinal);
            foreach (var entry in _equippedBySlot)
            {
                map[entry.Key] = entry.Value.PartId;
            }

            return map;
        }

        public bool TryGetEquipped(string slotId, out PartData part)
        {
            if (slotId == null)
            {
                part = null;
                return false;
            }

            return _equippedBySlot.TryGetValue(slotId, out part);
        }

        /// <summary>Equips the part into its slot. Returns the replaced part, or null if the slot was empty.</summary>
        public PartData Equip(PartData part)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            _equippedBySlot.TryGetValue(part.SlotId, out var previous);
            _equippedBySlot[part.SlotId] = part;

            if (previous != null)
            {
                PartRemoved?.Invoke(previous);
            }

            PartEquipped?.Invoke(part);
            return previous;
        }

        /// <summary>Clears the slot. Returns the removed part, or null if the slot was already empty.</summary>
        public PartData Remove(string slotId)
        {
            if (slotId == null || !_equippedBySlot.TryGetValue(slotId, out var removed))
            {
                return null;
            }

            _equippedBySlot.Remove(slotId);
            PartRemoved?.Invoke(removed);
            return removed;
        }
    }
}
