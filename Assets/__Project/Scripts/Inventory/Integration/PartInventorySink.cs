using System.Collections.Generic;
using CharacterSystem.Core;
using Inventory.Core;

namespace Inventory.Integration
{
    /// <summary>
    /// The Inventory-side implementation of the character system's shed-part port:
    /// parts shed by a body-plan change land in the player's part stash. The only
    /// bridge between the two layers (the character system never references Inventory).
    /// </summary>
    public class PartInventorySink : IShedPartSink
    {
        private readonly IPartInventoryModel _model;

        public PartInventorySink(IPartInventoryModel model)
        {
            _model = model;
        }

        public void Store(IReadOnlyList<string> partIds)
        {
            if (partIds == null)
            {
                return;
            }

            foreach (var partId in partIds)
            {
                if (!string.IsNullOrEmpty(partId))
                {
                    _model.Add(partId);
                }
            }
        }
    }
}
