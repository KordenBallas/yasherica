using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// The player's stash of body parts that are owned but not on the body — today filled
    /// only by parts shed during a body-plan change (body-plan-skeleton-swap.md FR8).
    /// A multiset of part definition ids: parts have no per-instance state, so identical
    /// ids simply accumulate. Re-installing from this stash is a deferred flow (ROADMAP).
    /// </summary>
    public interface IPartInventoryModel
    {
        /// <summary>All stored part ids, in insertion order; duplicates allowed.</summary>
        IReadOnlyList<string> PartIds { get; }

        event Action<string> OnPartAdded;

        void Add(string partId);
    }
}
