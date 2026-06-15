using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Feeding workflow above the magic pot: artifacts are selected from the inventory into a
    /// feeding tray (any number, no recipes), then digested together. Mirrors the staging side of
    /// <see cref="ICraftingSession"/> but has no mutation dependency - turning the digested items
    /// into archetype contributions is the presenter's job.
    /// </summary>
    public interface IFeedingSession
    {
        /// <summary>Items currently in the feeding tray, in selection order.</summary>
        IReadOnlyList<ArtifactInstance> Tray { get; }

        /// <summary>Raised whenever the tray contents change.</summary>
        event Action OnTrayChanged;

        /// <summary>
        /// Moves the inventory item with the given instance id into the feeding tray.
        /// Returns false when the item is unknown.
        /// </summary>
        bool TrySelect(int instanceId);

        /// <summary>Returns the tray item with the given instance id to the inventory.</summary>
        bool TryUnselect(int instanceId);

        /// <summary>
        /// Empties the tray and returns its contents (a snapshot) WITHOUT putting them back in the
        /// inventory - the caller digests them. They already left the inventory when selected.
        /// </summary>
        IReadOnlyList<ArtifactInstance> Consume();

        /// <summary>Returns every tray item to the inventory (e.g. on close or mode switch).</summary>
        void ReturnAll();
    }
}
