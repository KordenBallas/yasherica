using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Immutable snapshot of one artifact's function: its trait ids and its tier/potency.
    /// Substance and property vocabularies are merged here - the fusion grammar and all
    /// scoring are axis-agnostic; the axis split exists only on the authoring surface.
    /// Normalization drops null/empty ids, deduplicates, and orders ordinally so every
    /// downstream computation is deterministic.
    /// </summary>
    public class ArtifactTraitProfile
    {
        public static readonly ArtifactTraitProfile Empty =
            new ArtifactTraitProfile(new List<string>(), new HashSet<string>(), 0);

        private readonly List<string> _traits;
        private readonly HashSet<string> _traitSet;

        /// <summary>Trait ids in ordinal order, deduplicated.</summary>
        public IReadOnlyList<string> Traits => _traits;

        /// <summary>Potency: 0 = raw find, higher = crafted/refined.</summary>
        public int Tier { get; }

        private ArtifactTraitProfile(List<string> traits, HashSet<string> traitSet, int tier)
        {
            _traits = traits;
            _traitSet = traitSet;
            Tier = tier;
        }

        public static ArtifactTraitProfile Create(IEnumerable<string> traitIds, int tier)
        {
            var set = new HashSet<string>();
            var ordered = new List<string>();

            if (traitIds != null)
            {
                foreach (var id in traitIds)
                {
                    if (string.IsNullOrEmpty(id) || !set.Add(id))
                    {
                        continue;
                    }

                    ordered.Add(id);
                }
            }

            ordered.Sort(System.StringComparer.Ordinal);
            return new ArtifactTraitProfile(ordered, set, tier < 0 ? 0 : tier);
        }

        public bool Has(string traitId)
        {
            return !string.IsNullOrEmpty(traitId) && _traitSet.Contains(traitId);
        }
    }
}
