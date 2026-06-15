using System;
using System.Collections.Generic;
using System.Linq;

namespace Mutation.Core
{
    /// <summary>
    /// Default <see cref="IMutationTally"/>: a mutable per-stage accumulator of archetype weights.
    /// Depends only on <see cref="ArtifactArchetypeProfile"/> (Core -> Core), so it stays free of
    /// UnityEngine and fully unit-testable (CLAUDE.md §2, §10).
    /// </summary>
    public sealed class MutationTally : IMutationTally
    {
        private readonly Dictionary<string, float> _totals = new Dictionary<string, float>();

        public IReadOnlyDictionary<string, float> Totals => _totals;

        public bool IsEmpty => _totals.Count == 0;

        public event Action OnChanged;

        public void Add(ArtifactArchetypeProfile profile)
        {
            if (profile == null || profile.IsEmpty)
            {
                return;
            }

            foreach (var entry in profile.Weights)
            {
                _totals.TryGetValue(entry.Key, out var current);
                _totals[entry.Key] = current + entry.Value;
            }

            OnChanged?.Invoke();
        }

        public float TotalFor(string archetypeId)
        {
            if (string.IsNullOrEmpty(archetypeId))
            {
                return 0f;
            }

            return _totals.TryGetValue(archetypeId, out var weight) ? weight : 0f;
        }

        public IReadOnlyList<string> Dominant(int count)
        {
            if (count <= 0 || _totals.Count == 0)
            {
                return Array.Empty<string>();
            }

            // Weight desc, then ordinal id asc for a stable, deterministic order on ties.
            return _totals
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(count)
                .Select(pair => pair.Key)
                .ToList();
        }

        public void Reset()
        {
            if (_totals.Count == 0)
            {
                return;
            }

            _totals.Clear();
            OnChanged?.Invoke();
        }
    }
}
