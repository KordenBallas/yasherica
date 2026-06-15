using System;

namespace Inventory.Core
{
    /// <summary>
    /// Shared, observable selection of the current <see cref="InventoryMode"/>. The crafting and
    /// feeding presenters both listen to a pot-bubble click; reading this state lets each presenter
    /// ignore clicks that belong to the other mode, so they never act on the same click.
    /// </summary>
    public interface IInventoryModeState
    {
        InventoryMode Mode { get; }

        /// <summary>Raised when the mode actually changes.</summary>
        event Action<InventoryMode> OnModeChanged;

        void SetMode(InventoryMode mode);
    }
}
