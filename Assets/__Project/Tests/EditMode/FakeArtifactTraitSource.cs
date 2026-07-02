using System.Collections.Generic;
using Inventory.Core;

namespace Tests.EditMode
{
    /// <summary>
    /// Shared test double for <see cref="IArtifactTraitSource"/>: a hand-built
    /// artifact pool with trait profiles, used by the fusion fixtures.
    /// </summary>
    internal sealed class FakeArtifactTraitSource : IArtifactTraitSource
    {
        private readonly List<ArtifactTraitEntry> _entries = new List<ArtifactTraitEntry>();
        private readonly Dictionary<string, ArtifactTraitProfile> _byId =
            new Dictionary<string, ArtifactTraitProfile>();

        public IReadOnlyList<ArtifactTraitEntry> All => _entries;

        public FakeArtifactTraitSource Add(string id, int tier, params string[] traits)
        {
            var profile = ArtifactTraitProfile.Create(traits, tier);
            _entries.Add(new ArtifactTraitEntry(id, profile));
            _byId[id] = profile;
            return this;
        }

        public bool TryGetProfile(string definitionId, out ArtifactTraitProfile profile)
        {
            profile = null;
            return definitionId != null && _byId.TryGetValue(definitionId, out profile);
        }
    }
}
