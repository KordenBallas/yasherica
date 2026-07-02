using System;
using System.Collections.Generic;
using Inventory.Core;
using Inventory.Data.Definitions;

namespace Inventory.Data
{
    /// <summary>
    /// The SO-to-Core bridge for artifact function: built once at install time
    /// from the artifact catalog, it exposes each authored artifact as a pure
    /// <see cref="ArtifactTraitProfile"/>. Substance and property authoring lists
    /// are merged (the grammar is axis-agnostic); null/broken trait references are
    /// skipped here and reported by the content validator.
    /// </summary>
    public class ArtifactTraitIndex : IArtifactTraitSource
    {
        private readonly List<ArtifactTraitEntry> _all;
        private readonly Dictionary<string, ArtifactTraitProfile> _byId;

        public IReadOnlyList<ArtifactTraitEntry> All => _all;

        public ArtifactTraitIndex(IArtifactCatalog artifacts)
        {
            if (artifacts == null)
            {
                throw new ArgumentNullException(nameof(artifacts));
            }

            _all = new List<ArtifactTraitEntry>(artifacts.All.Count);
            _byId = new Dictionary<string, ArtifactTraitProfile>(artifacts.All.Count);

            foreach (var artifact in artifacts.All)
            {
                if (artifact == null || string.IsNullOrEmpty(artifact.Id) || _byId.ContainsKey(artifact.Id))
                {
                    continue;
                }

                var profile = ArtifactTraitProfile.Create(CollectTraitIds(artifact), artifact.Tier);
                _all.Add(new ArtifactTraitEntry(artifact.Id, profile));
                _byId.Add(artifact.Id, profile);
            }

            _all.Sort((a, b) => string.CompareOrdinal(a.DefinitionId, b.DefinitionId));
        }

        public bool TryGetProfile(string definitionId, out ArtifactTraitProfile profile)
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                profile = null;
                return false;
            }

            return _byId.TryGetValue(definitionId, out profile);
        }

        private static IEnumerable<string> CollectTraitIds(ArtifactDefinition artifact)
        {
            var ids = new List<string>();
            AppendIds(ids, artifact.SubstanceTraits);
            AppendIds(ids, artifact.PropertyTraits);
            return ids;
        }

        private static void AppendIds(List<string> ids, IReadOnlyList<TraitDefinition> traits)
        {
            if (traits == null)
            {
                return;
            }

            foreach (var trait in traits)
            {
                if (trait != null && !string.IsNullOrEmpty(trait.Id))
                {
                    ids.Add(trait.Id);
                }
            }
        }
    }
}
