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

        void SetOpenButtonVisible(bool visible);
        void SetOpenButtonInteractable(bool interactable);
        void SetCloseButtonVisible(bool visible);
    }
}
