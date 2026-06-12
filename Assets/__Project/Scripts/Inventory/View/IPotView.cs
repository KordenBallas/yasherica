using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// Magic pot adapter contract: renders artifact bubbles inside the cauldron
    /// liquid on the detached inventory stage and reports bubble clicks.
    /// </summary>
    public interface IPotView
    {
        /// <summary>Raised with the clicked artifact's instance id.</summary>
        event Action<int> OnBubbleClicked;

        /// <summary>Half extents of the pot interior in local units (X right, Y up).</summary>
        Vector2 PotInteriorHalfExtents { get; }

        /// <summary>Half depth of the pot interior along local Z, in local units.</summary>
        float PotInteriorHalfDepth { get; }

        void ShowBubbles(IReadOnlyList<BubbleViewData> bubbles);

        /// <summary>
        /// Focused while the inventory is open: bubbles become clickable and the
        /// boil effect intensifies. Unfocused bubbles ignore clicks.
        /// </summary>
        void SetPotFocused(bool focused);
    }
}
