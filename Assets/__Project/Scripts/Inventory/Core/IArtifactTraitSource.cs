using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Pure port onto the authored artifact pool as trait profiles: the emergent
    /// fusion reads input profiles from here and selects its output over
    /// <see cref="All"/>. Implemented by the data layer over the artifact catalog.
    /// </summary>
    public interface IArtifactTraitSource
    {
        /// <summary>Every authored artifact, ordinal by definition id (deterministic).</summary>
        IReadOnlyList<ArtifactTraitEntry> All { get; }

        bool TryGetProfile(string definitionId, out ArtifactTraitProfile profile);
    }
}
