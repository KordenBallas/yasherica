using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;

namespace Core.Persistence
{
    /// <summary>
    /// Captures the Meta partition of the unified fact store (D20 horizon, via the vocabulary
    /// registry) and saves it through <see cref="IMetaMemoryStore"/>. Pure C#; the fact store is
    /// never mutated by a flush.
    /// </summary>
    public sealed class MetaMemoryFlushService : IMetaMemoryFlush
    {
        private readonly IMetaMemoryStore _store;
        private readonly IFactStore _facts;
        private readonly IFactKeyRegistry _registry;

        public MetaMemoryFlushService(IMetaMemoryStore store, IFactStore facts, IFactKeyRegistry registry)
        {
            _store = store;
            _facts = facts;
            _registry = registry;
        }

        public void Flush()
        {
            _store.Save(new MetaMemorySnapshot
            {
                Facts = FactStoreSnapshotMapper.Capture(_facts, _registry, FactHorizon.Meta)
            });
        }
    }
}
