using System;
using System.Collections.Generic;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Mutation.Core;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Builds the candidate-part list the variant scoring consumes from the authored character
    /// <see cref="PartDefinition"/>s. The only bridge from the character Data layer into the
    /// UnityEngine-free <see cref="MutationCandidatePart"/> records: it copies the slot/part ids,
    /// the per-trait affinity (summing duplicate ids, dropping empty ids and non-positive weights),
    /// and the rarity as an int tier. The choice icon stays in the Data layer (served by
    /// <see cref="TryGetIcon"/>) so Core holds no Unity types. The part's display label is its
    /// authored <see cref="PartDefinition.DisplayName"/>, falling back to the asset name when blank.
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
                var traitAffinity = BuildTraitAffinity(part.TraitAffinities);
                var displayName = string.IsNullOrEmpty(part.DisplayName) ? part.name : part.DisplayName;

                _candidates.Add(new MutationCandidatePart(
                    slotId,
                    part.Id,
                    displayName,
                    (int)part.Rarity,
                    traitAffinity));

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

        // Aggregation rules: sum duplicates, drop empty ids and non-positive weights.
        private static IReadOnlyDictionary<string, float> BuildTraitAffinity(
            IReadOnlyList<TraitAffinity> affinities)
        {
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

                var id = affinity.TraitId != null ? affinity.TraitId.Trim() : null;
                if (string.IsNullOrEmpty(id) || affinity.Weight <= 0f)
                {
                    continue;
                }

                map.TryGetValue(id, out var existing);
                map[id] = existing + affinity.Weight;
            }

            return map;
        }

        private static readonly IReadOnlyDictionary<string, float> EmptyAffinity =
            new Dictionary<string, float>(0);
    }
}
