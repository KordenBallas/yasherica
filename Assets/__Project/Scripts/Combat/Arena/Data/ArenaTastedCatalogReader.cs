using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Data;
using Core.Persistence;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;

namespace Combat.Arena.Data
{
    /// <summary>
    /// The Arena-side read of the tasted-forms catalog (P4-5 req 3 — read-only for Arena):
    /// pulls <c>world.&lt;partId&gt;.arena_tasted</c> straight from <c>meta.json</c>. The Arena
    /// scene deliberately loads the snapshot instead of bootstrapping the narrative fact store —
    /// the catalog is the only meta consumer here. Frame-changing parts are dropped (the first
    /// draft stays on the base body-plan), as are ids the part catalog no longer knows.
    /// </summary>
    public class ArenaTastedCatalogReader
    {
        private readonly IMetaMemoryStore _metaStore;
        private readonly IPartCatalog _partCatalog;

        public ArenaTastedCatalogReader(IMetaMemoryStore metaStore, IPartCatalog partCatalog)
        {
            _metaStore = metaStore;
            _partCatalog = partCatalog;
        }

        public IReadOnlyList<string> ReadLocalCatalog()
        {
            var tasted = ExtractTastedPartIds(_metaStore.LoadOrEmpty()?.Facts);
            return tasted
                .Where(IsDraftable)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Pure core: the tasted part ids recorded in a fact snapshot.</summary>
        public static List<string> ExtractTastedPartIds(FactStoreSnapshot snapshot)
        {
            var partIds = new List<string>();
            if (snapshot?.Entries == null)
            {
                return partIds;
            }

            foreach (var entry in snapshot.Entries)
            {
                if (entry.Namespace == WorldFacts.ArenaTasted.Namespace
                    && entry.Key == WorldFacts.ArenaTasted.Key
                    && entry.BoolValue
                    && !string.IsNullOrEmpty(entry.Subject))
                {
                    partIds.Add(entry.Subject);
                }
            }

            return partIds;
        }

        private bool IsDraftable(string partId)
        {
            return _partCatalog.TryGet(partId, out var part) && !part.GovernsBodyPlan;
        }
    }
}
