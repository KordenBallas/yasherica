using System;
using System.Collections.Generic;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Combat.Integration;
using Mutation.Core;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Builds the candidate-part list the variant scoring consumes from the authored character
    /// <see cref="PartDefinition"/>s. The only bridge from the character Data layer into the
    /// UnityEngine-free <see cref="MutationCandidatePart"/> records: it copies the slot/part ids,
    /// the per-trait affinity (summing duplicate ids, dropping empty ids and non-positive weights),
    /// and the rarity as an int tier. The choice icon and per-part card data (name, icon, tier,
    /// granted abilities — resolved through the combat <see cref="IPartAbilityResolver"/> so the
    /// card shows exactly the ability set combat composes) stay in the Data layer, served by
    /// <see cref="TryGetIcon"/>/<see cref="TryGetCardData"/> so Core holds no Unity types. The
    /// part's display label is its authored <see cref="PartDefinition.DisplayName"/>, falling back
    /// to the asset name when blank.
    /// </summary>
    public class MutationPartCatalog : IMutationPartCatalog
    {
        private readonly List<MutationCandidatePart> _candidates;
        private readonly Dictionary<string, Sprite> _iconByPart;
        private readonly Dictionary<string, MutationPartCardData> _cardDataByPart;

        public IReadOnlyList<MutationCandidatePart> AllCandidates => _candidates;

        public MutationPartCatalog(IPartCatalog partCatalog, IPartAbilityResolver abilityResolver)
        {
            if (partCatalog == null)
            {
                throw new ArgumentNullException(nameof(partCatalog));
            }

            if (abilityResolver == null)
            {
                throw new ArgumentNullException(nameof(abilityResolver));
            }

            var parts = partCatalog.All;
            _candidates = new List<MutationCandidatePart>(parts.Count);
            _iconByPart = new Dictionary<string, Sprite>(parts.Count, StringComparer.Ordinal);
            _cardDataByPart = new Dictionary<string, MutationPartCardData>(parts.Count, StringComparer.Ordinal);

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

                if (!_cardDataByPart.ContainsKey(part.Id))
                {
                    _cardDataByPart.Add(part.Id, new MutationPartCardData(
                        displayName,
                        part.ChoiceIcon,
                        (int)part.Rarity,
                        BuildAbilities(abilityResolver, part.Id)));
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

        public bool TryGetCardData(string partId, out MutationPartCardData cardData)
        {
            if (string.IsNullOrEmpty(partId))
            {
                cardData = null;
                return false;
            }

            return _cardDataByPart.TryGetValue(partId, out cardData);
        }

        // Route through the combat resolver (single-part query) so the card's ability list
        // matches the set combat composes — same null-filtering and asset-ref dedupe.
        private static IReadOnlyList<MutationAbilityInfo> BuildAbilities(
            IPartAbilityResolver abilityResolver, string partId)
        {
            var set = abilityResolver.Resolve(new[] { partId });
            if (set.IsEmpty)
            {
                return Array.Empty<MutationAbilityInfo>();
            }

            var abilities = new List<MutationAbilityInfo>(
                set.ActiveAbilities.Count + set.PassiveAbilities.Count);
            foreach (var ability in set.ActiveAbilities)
            {
                abilities.Add(new MutationAbilityInfo(
                    ability.Name, ability.Description, ability.Icon, isPassive: false,
                    isLine: ability.Shape == Combat.Core.AbilityShapeType.Line,
                    lineLength: ability.LineLength,
                    ringRadius: ability.RingRadius,
                    animationTrigger: ability.AnimationTrigger));
            }

            foreach (var passive in set.PassiveAbilities)
            {
                abilities.Add(new MutationAbilityInfo(
                    passive.Name, passive.Description, passive.Icon, isPassive: true));
            }

            return abilities;
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
