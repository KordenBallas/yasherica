using System.Collections.Generic;
using Inventory.Data.Definitions;

namespace Inventory.Data
{
    /// <summary>
    /// Lookup from artifact definition id to its authored data.
    /// Views and presenters use this to resolve icons/names for runtime instances.
    /// </summary>
    public interface IArtifactCatalog
    {
        IReadOnlyList<ArtifactDefinition> All { get; }

        bool TryGet(string definitionId, out ArtifactDefinition definition);
    }
}
