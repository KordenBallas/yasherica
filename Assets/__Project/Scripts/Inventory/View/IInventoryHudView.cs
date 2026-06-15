using System;

namespace Inventory.View
{
    /// <summary>
    /// HUD adapter contract: the on-screen button opening the belly inventory
    /// and the close control shown while it is open.
    /// </summary>
    public interface IInventoryHudView
    {
        event Action OnOpenClicked;
        event Action OnCloseClicked;

        /// <summary>Raised when the player toggles the feeding mode on or off.</summary>
        event Action OnFeedModeToggled;

        void SetOpenButtonVisible(bool visible);
        void SetOpenButtonInteractable(bool interactable);
        void SetCloseButtonVisible(bool visible);

        /// <summary>Shows/hides the feed-mode toggle (only meaningful while the inventory is open).</summary>
        void SetFeedModeToggleVisible(bool visible);

        /// <summary>Reflects whether feeding mode is currently active.</summary>
        void SetFeedModeActive(bool active);
    }
}
