using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Default in-memory part stash. Mirrors <see cref="InventoryModel"/>'s multiset shape,
    /// but keyed by part definition id alone: parts carry no per-instance state yet.
    /// </summary>
    public class PartInventoryModel : IPartInventoryModel
    {
        private readonly List<string> _partIds = new List<string>();

        public IReadOnlyList<string> PartIds => _partIds;

        public event Action<string> OnPartAdded;

        public void Add(string partId)
        {
            if (string.IsNullOrEmpty(partId))
            {
                throw new ArgumentException("Part id must not be null or empty.", nameof(partId));
            }

            _partIds.Add(partId);
            OnPartAdded?.Invoke(partId);
        }
    }
}
