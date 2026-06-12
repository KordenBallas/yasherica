namespace Inventory.Core
{
    /// <summary>
    /// Observable states of a crafting session.
    /// Crafting covers the period between staging the Nth item and resolving the
    /// combine, so the presentation layer can animate the merge and the player
    /// can still cancel it.
    /// </summary>
    public enum CraftingState
    {
        Idle,
        Selecting,
        Crafting,
        ResultReady
    }
}
