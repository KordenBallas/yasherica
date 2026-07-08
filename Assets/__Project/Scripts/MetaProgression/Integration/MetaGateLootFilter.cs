using System;
using System.Collections.Generic;
using Inventory.Data;
using Loot.Core;
using MetaProgression.Core;

namespace MetaProgression.Integration
{
    /// <summary>
    /// The world-loot eligibility gate (meta-progression FR3): a meta-gated artifact whose deed is
    /// not yet met is invisible to every biome loot table (platform finds, enemy drops, quest
    /// tables). The locked set is projected once from the artifact catalog against the frozen
    /// per-run vocabulary, so per-roll checks are a set lookup. Unknown or unmarked artifact ids
    /// pass (base default, FR15); a scene without the meta bindings (null vocabulary) filters
    /// nothing.
    /// </summary>
    public sealed class MetaGateLootFilter : ILootEntryFilter
    {
        private readonly HashSet<string> _lockedArtifactIds;

        public MetaGateLootFilter(IArtifactCatalog artifacts, IMetaVocabulary vocabulary)
        {
            _lockedArtifactIds = BuildLockedSet(artifacts, vocabulary);
        }

        public bool IsEligible(LootEntryData entry, LootRollContext context)
        {
            return entry == null || !_lockedArtifactIds.Contains(entry.ArtifactId);
        }

        private static HashSet<string> BuildLockedSet(IArtifactCatalog artifacts, IMetaVocabulary vocabulary)
        {
            var locked = new HashSet<string>(StringComparer.Ordinal);
            if (artifacts?.All == null || vocabulary == null)
            {
                return locked;
            }

            foreach (var definition in artifacts.All)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    continue;
                }

                if (!vocabulary.IsUnlocked(definition.Id, definition.MetaGating.ToCore()))
                {
                    locked.Add(definition.Id);
                }
            }

            return locked;
        }
    }
}
