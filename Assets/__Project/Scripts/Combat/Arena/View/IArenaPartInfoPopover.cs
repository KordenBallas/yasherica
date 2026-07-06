using System;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>
    /// The clicked-part popover (G4 req 11): part identity + ability rows (hover → the shared
    /// ability preview) + the Draft action when the pick is legal right now.
    /// </summary>
    public interface IArenaPartInfoPopover
    {
        /// <summary>Hover over ability row <c>index</c> of the currently shown part.</summary>
        event Action<int, Vector2, bool> AbilityHovered;

        /// <summary>The Draft button of the currently shown part.</summary>
        event Action DraftClicked;

        void Show(ArenaPartInfoViewData data, Vector2 screenPosition);
        void Hide();
    }
}
