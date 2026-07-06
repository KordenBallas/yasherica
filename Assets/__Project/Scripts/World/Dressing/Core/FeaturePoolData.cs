using System;
using System.Collections.Generic;

namespace World.Dressing.Core
{
    /// <summary>
    /// The pure feature pool of one biome-feature kit (dressing-kit brief FR1): the entries a biome
    /// scatters on its platforms. Which meshes those entries mean is the kit asset's business —
    /// placement only ever sees kinds, weights, and scale ranges, which is what keeps
    /// "swapping a kit changes only appearance, not placement" true by construction (FR7).
    /// </summary>
    public sealed class FeaturePoolData
    {
        public FeaturePoolData(string kitId, IReadOnlyList<FeatureEntryData> entries)
        {
            KitId = kitId ?? string.Empty;
            Entries = entries ?? Array.Empty<FeatureEntryData>();
        }

        /// <summary>The owning kit's stable id (logs + spawner prefab lookup).</summary>
        public string KitId { get; }

        /// <summary>Index-aligned with the kit asset's feature list.</summary>
        public IReadOnlyList<FeatureEntryData> Entries { get; }

        public bool IsEmpty => Entries.Count == 0;
    }
}
