using System;

namespace Inventory.Core
{
    /// <summary>
    /// Default <see cref="IInventoryModeState"/>: a single shared mode value. Starts in
    /// <see cref="InventoryMode.Crafting"/> (the pot's default behaviour).
    /// </summary>
    public sealed class InventoryModeState : IInventoryModeState
    {
        public InventoryMode Mode { get; private set; } = InventoryMode.Crafting;

        public event Action<InventoryMode> OnModeChanged;

        public void SetMode(InventoryMode mode)
        {
            if (mode == Mode)
            {
                return;
            }

            Mode = mode;
            OnModeChanged?.Invoke(mode);
        }
    }
}
