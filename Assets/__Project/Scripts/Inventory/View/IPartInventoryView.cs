using System.Collections.Generic;

namespace Inventory.View
{
    /// <summary>
    /// Read-only readout of the player's stored body parts (shed by body-plan changes).
    /// An empty list hides the panel entirely; re-installing from the stash is a
    /// deferred flow (ROADMAP), so the view exposes no interactions.
    /// </summary>
    public interface IPartInventoryView
    {
        void ShowParts(IReadOnlyList<PartInventoryItemViewData> items);
    }
}
