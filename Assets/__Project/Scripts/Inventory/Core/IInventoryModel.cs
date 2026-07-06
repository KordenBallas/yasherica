using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Container-based inventory: an unordered multiset of artifact instances (no slots).
    /// Also owns instance id generation so every artifact created anywhere has a unique id.
    /// </summary>
    public interface IInventoryModel
    {
        IReadOnlyList<ArtifactInstance> Items { get; }

        event Action<ArtifactInstance> OnItemAdded;
        event Action<ArtifactInstance> OnItemRemoved;

        /// <summary>Creates a new instance of the given definition and adds it to the container.</summary>
        ArtifactInstance Add(string definitionId);

        /// <summary>Re-adds an existing instance (e.g. returned from a crafting session).</summary>
        void Return(ArtifactInstance instance);

        /// <summary>Removes the instance with the given id. Returns false if not present.</summary>
        bool Remove(int instanceId);

        bool TryGet(int instanceId, out ArtifactInstance instance);

        /// <summary>
        /// Creates an instance with a unique id WITHOUT adding it to the container.
        /// Used for crafting results that live above the pot until collected.
        /// </summary>
        ArtifactInstance CreateDetachedInstance(string definitionId);

        /// <summary>The next instance id this model would mint — captured by the save layer so a
        /// restored run keeps minting unique ids.</summary>
        int NextInstanceId { get; }

        /// <summary>Replaces the whole container from a save image (P2-2 restore). Fires
        /// <see cref="OnItemAdded"/> per item so presenters stay consistent.</summary>
        void RestoreFrom(IReadOnlyList<ArtifactInstance> items, int nextInstanceId);
    }
}
