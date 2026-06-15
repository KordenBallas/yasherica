using System;
using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free snapshot of how much one artifact contributes to each
    /// creature archetype axis (archetype id -> accumulated weight).
    /// Built once from authored data via <see cref="Create"/>; consumed by the per-level
    /// mutation tally (see ROADMAP "M1 - Per-level mutation tally").
    /// </summary>
    public sealed class ArtifactArchetypeProfile
    {
        private static readonly ArtifactArchetypeProfile EmptyProfile =
            new ArtifactArchetypeProfile(new Dictionary<string, float>());

        private readonly IReadOnlyDictionary<string, float> _weights;

        public static ArtifactArchetypeProfile Empty => EmptyProfile;

        /// <summary>Archetype id -> total positive weight this artifact contributes.</summary>
        public IReadOnlyDictionary<string, float> Weights => _weights;

        public bool IsEmpty => _weights.Count == 0;

        private ArtifactArchetypeProfile(IReadOnlyDictionary<string, float> weights)
        {
            _weights = weights;
        }

        /// <summary>
        /// Aggregates raw (archetypeId, weight) entries into a profile: trims ids, drops
        /// empty ids and non-positive weights, and sums duplicate ids so authoring order
        /// and repetition do not matter.
        /// </summary>
        public static ArtifactArchetypeProfile Create(IEnumerable<KeyValuePair<string, float>> entries)
        {
            if (entries == null)
            {
                return EmptyProfile;
            }

            var accumulated = new Dictionary<string, float>();
            foreach (var entry in entries)
            {
                var id = entry.Key?.Trim();
                if (string.IsNullOrEmpty(id) || entry.Value <= 0f)
                {
                    continue;
                }

                accumulated.TryGetValue(id, out var current);
                accumulated[id] = current + entry.Value;
            }

            return accumulated.Count == 0
                ? EmptyProfile
                : new ArtifactArchetypeProfile(accumulated);
        }

        /// <summary>
        /// Sums several profiles into one (e.g. the cumulative archetype readout for a set of
        /// artifacts staged in the feeding tray). Flattens every profile's weights and reuses
        /// <see cref="Create"/>, so the same aggregation rules apply; null and empty profiles
        /// are skipped.
        /// </summary>
        public static ArtifactArchetypeProfile Combine(IEnumerable<ArtifactArchetypeProfile> profiles)
        {
            if (profiles == null)
            {
                return EmptyProfile;
            }

            return Create(Flatten(profiles));
        }

        private static IEnumerable<KeyValuePair<string, float>> Flatten(
            IEnumerable<ArtifactArchetypeProfile> profiles)
        {
            foreach (var profile in profiles)
            {
                if (profile == null || profile.IsEmpty)
                {
                    continue;
                }

                foreach (var entry in profile.Weights)
                {
                    yield return entry;
                }
            }
        }

        /// <summary>Weight contributed to <paramref name="archetypeId"/>, or 0 if none.</summary>
        public float WeightFor(string archetypeId)
        {
            if (string.IsNullOrEmpty(archetypeId))
            {
                return 0f;
            }

            return _weights.TryGetValue(archetypeId, out var weight) ? weight : 0f;
        }
    }
}
