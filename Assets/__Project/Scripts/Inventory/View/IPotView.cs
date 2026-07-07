using System;
using System.Collections.Generic;

namespace Inventory.View
{
    /// <summary>
    /// Magic pot adapter contract: renders artifact bubbles on their stable
    /// spots inside the cauldron liquid on the detached inventory stage and
    /// reports bubble clicks.
    /// </summary>
    public interface IPotView
    {
        /// <summary>Raised with the clicked artifact's instance id.</summary>
        event Action<int> OnBubbleClicked;

        /// <summary>
        /// Diffs the shown bubbles by instance id: existing bubbles keep their
        /// views (untouched artifacts never move), new ones drop in, missing
        /// ones pop.
        /// </summary>
        void ShowBubbles(IReadOnlyList<BubbleViewData> bubbles);

        /// <summary>
        /// Focused while the inventory is open: bubbles become clickable and the
        /// boil effect intensifies. Unfocused bubbles ignore clicks.
        /// </summary>
        void SetPotFocused(bool focused);
    }
}
