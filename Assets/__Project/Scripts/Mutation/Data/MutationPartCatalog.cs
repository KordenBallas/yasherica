using System;
using System.Collections.Generic;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Mutation.Core;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Builds the candidate-part list the mutation scoring consumes from the authored character
    /// <see cref="PartDefinition"/>s. The only bridge from the character Data layer into the
    /// UnityEngine-free <see cref="MutationCandidatePart"/> records: it copies the slot/part ids,
    /// the per-archetype affinity (summing duplicate ids, dropping empty ids and non-positive
    /// weights), the rarity as an int tier, and the dominant-affinity archetype id (for the choice
    /// tint). The choice icon stays in the Data layer (served by <see cref="TryGetIcon"/>) so Core
    /// holds no Unity types. The part's display label is its authored
    /// <see cref="PartDefinition.DisplayName"/>, falling back to the asset name when blank.
    /// </summary>
    public class MutationPartCatalog : IMutationPartCatalog
    {
        private readonly List<MutationCandidatePart> _candidates;
        private readonly Dictionary<string, Sprite> _iconByPart;

        public IReadOnlyList<MutationCandidatePart> AllCandidates => _candidates;

        public MutationPartCatalog(IPartCatalog partCatalog)
        {
            if (partCatalog == null)
            {
                throw new ArgumentNullException(nameof(partCatalog));
            }

            var parts = partCatalog.All;
            _candidates = new List<MutationCandidatePart>(parts.Count);
            _iconByPart = new Dictionary<string, Sprite>(parts.Count, StringComparer.Ordinal);

            foreach (var part in parts)
            {
                if (part == null || string.IsNullOrEmpty(part.Id))
                {
                    continue;
                }

                var slotId = part.Slot != null ? part.Slot.Id : null;
                var affinity = BuildAffinity(part.ArchetypeAffinities, out var dominantArchetypeId);
                var displayName = string.IsNullOrEmpty(part.DisplayName) ? part.name : part.DisplayName;

                _candidates.Add(new MutationCandidatePart(
                    slotId,
                    part.Id,
                    displayName,
                    affinity,
                    (int)part.Rarity,
                    dominantArchetypeId));

                if (part.ChoiceIcon != null && !_iconByPart.ContainsKey(part.Id))
                {
                    _iconByPart.Add(part.Id, part.ChoiceIcon);
                }
            }
        }

        public bool TryGetIcon(string partId, out Sprite icon)
        {
            if (string.IsNullOrEmpty(partId))
            {
                icon = null;
                return false;
            }

            return _iconByPart.TryGetValue(partId, out icon);
        }

        private static IReadOnlyDictionary<string, float> BuildAffinity(
            IReadOnlyList<ArchetypeAffinity> affinities,
            out string dominantArchetypeId)
        {
            dominantArchetypeId = null;
            if (affinities == null || affinities.Count == 0)
            {
                return EmptyAffinity;
            }

            var map = new Dictionary<string, float>(affinities.Count, StringComparer.Ordinal);
            foreach (var affinity in affinities)
            {
                if (affinity == null)
                {
                    continue;
                }

                var id = affinity.ArchetypeId != null ? affinity.ArchetypeId.Trim() : null;
                if (string.IsNullOrEmpty(id) || affinity.Weight <= 0f)
                {
                    continue;
                }

                map.TryGetValue(id, out var existing);
                map[id] = existing + affinity.Weight;
            }

            dominantArchetypeId = Dominant(map);
            return map;
        }

        // Highest-weight archetype, with an ordinal-id tie-break so the tint is deterministic.
        private static string Dominant(Dictionary<string, float> map)
        {
            string best = null;
            var bestWeight = float.NegativeInfinity;
            foreach (var entry in map)
            {
                if (entry.Value > bestWeight ||
                    (entry.Value == bestWeight && string.CompareOrdinal(entry.Key, best) < 0))
                {
                    best = entry.Key;
                    bestWeight = entry.Value;
                }
            }

            return best;
        }

        private static readonly IReadOnlyDictionary<string, float> EmptyAffinity =
            new Dictionary<string, float>(0);
    }
}
