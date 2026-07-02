using System.Collections.Generic;
using Inventory.Data.Definitions;

namespace Inventory.Data
{
    /// <summary>
    /// Lookup from trait id to its authored definition. Validators and future UI
    /// use this to resolve names/axes for trait ids carried by pure records.
    /// </summary>
    public interface ITraitCatalog
    {
        IReadOnlyList<TraitDefinition> All { get; }

        bool Contains(string traitId);

        bool TryGet(string traitId, out TraitDefinition definition);
    }
}
