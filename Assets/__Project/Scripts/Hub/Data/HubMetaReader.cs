using Core.Persistence;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;

namespace Hub.Data
{
    /// <summary>
    /// The Hub's read of the cross-run memory (O1): pulls <c>world.run_count</c> straight from the
    /// <c>meta.json</c> snapshot — the Arena precedent (<c>ArenaTastedCatalogReader</c>): the Hub
    /// scene deliberately loads the snapshot instead of bootstrapping the narrative fact store.
    /// The upcoming run's index (count + 1) salts the offer draw and the voice-line pick.
    /// </summary>
    public class HubMetaReader
    {
        private readonly IMetaMemoryStore _metaStore;

        public HubMetaReader(IMetaMemoryStore metaStore)
        {
            _metaStore = metaStore;
        }

        public int ReadRunCount()
        {
            return ExtractRunCount(_metaStore.LoadOrEmpty()?.Facts);
        }

        /// <summary>Pure core: the persisted run counter in a fact snapshot (0 when absent).</summary>
        public static int ExtractRunCount(FactStoreSnapshot snapshot)
        {
            if (snapshot?.Entries == null)
            {
                return 0;
            }

            foreach (var entry in snapshot.Entries)
            {
                if (entry.Namespace == WorldFacts.RunCount.Namespace
                    && entry.Key == WorldFacts.RunCount.Key
                    && entry.Type == FactValueType.Int)
                {
                    return (int)entry.IntValue;
                }
            }

            return 0;
        }
    }
}
