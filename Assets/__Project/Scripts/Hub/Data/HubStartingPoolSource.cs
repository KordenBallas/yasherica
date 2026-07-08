using System.Collections.Generic;
using CharacterSystem.Data;
using Combat.Arena.Data;
using Hub.Core;
using MetaProgression.Core;

namespace Hub.Data
{
    /// <summary>
    /// Builds the Hub's starting-part pool (O1): the tasted-forms catalog (every part the hero has
    /// ever worn, meta-persistent) already filtered to base-skeleton fits by
    /// <see cref="ArenaTastedCatalogReader"/>, enriched with the race tag / slot / active-ability
    /// facts the selector's variety draw trades on. The dig pool is Tasted ∩ Eligible (Track R,
    /// owner call 2026-07-07): tasting stays the membership rule — the dig remains "what the
    /// cauldron has tasted" — while the meta gate withholds a tasted-but-locked token (e.g. a form
    /// whose deed is a spine milestone) until its deed lands. Cross-system reuse of the Arena
    /// reader is a deliberate KISS call — relocating it to a shared home is ROADMAP debt.
    /// </summary>
    public class HubStartingPoolSource : IStartingPartPoolSource
    {
        private readonly ArenaTastedCatalogReader _tastedReader;
        private readonly IPartCatalog _partCatalog;
        private readonly IMetaVocabulary _vocabulary;

        public HubStartingPoolSource(
            ArenaTastedCatalogReader tastedReader,
            IPartCatalog partCatalog,
            IMetaVocabulary vocabulary = null)
        {
            _tastedReader = tastedReader;
            _partCatalog = partCatalog;
            _vocabulary = vocabulary;
        }

        public IReadOnlyList<StartingPartCandidate> BuildPool()
        {
            var tastedIds = _tastedReader.ReadLocalCatalog();
            var pool = new List<StartingPartCandidate>(tastedIds.Count);
            foreach (var partId in tastedIds)
            {
                if (!_partCatalog.TryGet(partId, out var part))
                {
                    continue;
                }

                var gate = part.MetaGating.ToCore();
                if (_vocabulary != null && !_vocabulary.IsUnlocked(part.Id, gate))
                {
                    continue;
                }

                pool.Add(new StartingPartCandidate(
                    part.Id,
                    part.RaceId,
                    part.Slot != null ? part.Slot.Id : string.Empty,
                    part.ActiveAbilities.Count > 0,
                    TraitIds(part),
                    gate.DrawWeight));
            }

            return pool;
        }

        // The part's positive trait-affinity ids — the artifact-family axis the direction bias
        // matches against (FR8).
        private static IReadOnlyList<string> TraitIds(CharacterSystem.Data.Definitions.PartDefinition part)
        {
            var affinities = part.TraitAffinities;
            if (affinities.Count == 0)
            {
                return null;
            }

            var ids = new List<string>(affinities.Count);
            foreach (var affinity in affinities)
            {
                if (affinity != null && !string.IsNullOrEmpty(affinity.TraitId) && affinity.Weight > 0f)
                {
                    ids.Add(affinity.TraitId);
                }
            }

            return ids.Count > 0 ? ids : null;
        }
    }
}
