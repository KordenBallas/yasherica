namespace Inventory.Core
{
    /// <summary>
    /// Which interaction a pot-bubble click performs while the inventory is open. Crafting stages
    /// bubbles to combine above the pot; Feeding selects bubbles into the feeding tray to digest
    /// them into the mutation tally. The two are mutually exclusive so a single click is unambiguous.
    /// </summary>
    public enum InventoryMode
    {
        Crafting,
        Feeding
    }
}
