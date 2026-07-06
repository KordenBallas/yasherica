using System.Collections.Generic;
using CharacterSystem.Data;
using Combat.Arena.Data;
using Hub.Core;

namespace Hub.Data
{
    /// <summary>
    /// Builds the Hub's starting-part pool (O1): the tasted-forms catalog (every part the hero has
    /// ever worn, meta-persistent) already filtered to base-skeleton fits by
    /// <see cref="ArenaTastedCatalogReader"/>, enriched with the race tag / slot / active-ability
    /// facts the selector's variety draw trades on. Cross-system reuse of the Arena reader is a
    /// deliberate KISS call — relocating it to a shared home is ROADMAP debt.
    /// </summary>
    public class HubStartingPoolSource : IStartingPartPoolSource
    {
        private readonly ArenaTastedCatalogReader _tastedReader;
        private readonly IPartCatalog _partCatalog;

        public HubStartingPoolSource(ArenaTastedCatalogReader tastedReader, IPartCatalog partCatalog)
        {
            _tastedReader = tastedReader;
            _partCatalog = partCatalog;
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

                pool.Add(new StartingPartCandidate(
                    part.Id,
                    part.RaceId,
                    part.Slot != null ? part.Slot.Id : string.Empty,
                    part.ActiveAbilities.Count > 0));
            }

            return pool;
        }
    }
}
