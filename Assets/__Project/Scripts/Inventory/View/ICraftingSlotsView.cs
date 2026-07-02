using System;
using System.Collections.Generic;

namespace Inventory.View
{
    /// <summary>
    /// Adapter contract for the crafting area above the pot: staged artifacts,
    /// the merge animation, the crafted result, and the success effects.
    /// </summary>
    public interface ICraftingSlotsView
    {
        event Action OnResultClicked;

        /// <summary>Raised when the player clicks a staged artifact.</summary>
        event Action<int> OnStagedItemClicked;

        /// <summary>
        /// Raised when the merge travel animation finishes. Never raised after
        /// ShowStagedItems rebuilds the slots and aborts the animation.
        /// </summary>
        event Action OnMergeCompleted;

        /// <summary>Rebuilds the staged views, aborting any running merge animation.</summary>
        void ShowStagedItems(IReadOnlyList<ArtifactViewData> items);

        /// <summary>
        /// Animates the staged bubbles from their slots toward the result anchor,
        /// then raises OnMergeCompleted (always deferred by at least one frame).
        /// </summary>
        void PlayMergeAnimation();

        void ShowResult(ArtifactViewData result);

        /// <param name="collected">True plays the drop-into-pot animation before clearing.</param>
        void ClearResult(bool collected);

        void PlaySuccessPuff();
    }
}
