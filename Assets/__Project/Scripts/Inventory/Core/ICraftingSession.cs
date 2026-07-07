using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Crafting workflow above the magic pot: artifacts are staged from the inventory,
    /// combined automatically once enough are staged, and the result is collected back
    /// into the inventory.
    /// </summary>
    public interface ICraftingSession
    {
        CraftingState State { get; }

        /// <summary>Items currently staged above the pot, in selection order.</summary>
        IReadOnlyList<ArtifactInstance> StagedItems { get; }

        /// <summary>Crafted result waiting above the pot, or null when there is none.</summary>
        ArtifactInstance PendingResult { get; }

        event Action<ArtifactInstance> OnItemStaged;

        /// <summary>Raised when enough items are staged and the combine awaits ResolveCraft.</summary>
        event Action<IReadOnlyList<ArtifactInstance>> OnCraftingStarted;

        /// <summary>
        /// Raised when a combine resolves with the crafted result. The flag tells
        /// whether it matched an authored signature recipe (vs an emergent fusion).
        /// Every combine resolves - there is no fail path.
        /// </summary>
        event Action<ArtifactInstance, bool> OnCraftSucceeded;

        /// <summary>Raised when a staged item is returned to the inventory by TryUnstage.</summary>
        event Action<ArtifactInstance> OnItemUnstaged;

        event Action<ArtifactInstance> OnResultCollected;
        event Action<IReadOnlyList<ArtifactInstance>> OnSessionCleared;

        /// <summary>
        /// Moves the inventory item with the given instance id into the crafting slots.
        /// A hovering result is auto-collected into the inventory first (Track F
        /// continuous flow). Returns false when the item is unknown or a combine is
        /// already awaiting resolution.
        /// </summary>
        bool TrySelect(int instanceId);

        /// <summary>
        /// Resolves the pending combine through the fusion resolver (signature
        /// recipe first, else emergent). Only valid in the Crafting state; returns
        /// false otherwise (e.g. the craft was cancelled before the merge
        /// animation completed).
        /// </summary>
        bool ResolveCraft();

        /// <summary>
        /// Returns the staged item with the given instance id to the inventory.
        /// In the Crafting state this cancels the pending combine. Returns false
        /// when the id is not staged or the state forbids unstaging.
        /// </summary>
        bool TryUnstage(int instanceId);

        /// <summary>Drops the pending crafted result into the inventory.</summary>
        bool TryCollectResult();

        /// <summary>Returns all staged items and any pending result to the inventory.</summary>
        void ReturnAll();
    }
}
