using MetaProgression.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;

namespace Core.Persistence
{
    /// <summary>
    /// Captures the Meta partition of the unified fact store (D20 horizon, via the vocabulary
    /// registry) plus the cross-run direction ledger (meta-progression FR8) and saves them through
    /// <see cref="IMetaMemoryStore"/>. Pure C#; neither the fact store nor the ledger is mutated by
    /// a flush. The ledger is pruned to the configured retention at capture, so the file never
    /// grows beyond the direction window's needs.
    /// </summary>
    public sealed class MetaMemoryFlushService : IMetaMemoryFlush
    {
        private readonly IMetaMemoryStore _store;
        private readonly IFactStore _facts;
        private readonly IFactKeyRegistry _registry;
        private readonly RunLedger _ledger;
        private readonly MetaProgressionSettings _settings;

        public MetaMemoryFlushService(
            IMetaMemoryStore store,
            IFactStore facts,
            IFactKeyRegistry registry,
            RunLedger ledger = null,
            MetaProgressionSettings settings = null)
        {
            _store = store;
            _facts = facts;
            _registry = registry;
            _ledger = ledger;
            _settings = settings ?? MetaProgressionSettings.Defaults;
        }

        public void Flush()
        {
            _store.Save(new MetaMemorySnapshot
            {
                Facts = FactStoreSnapshotMapper.Capture(_facts, _registry, FactHorizon.Meta),
                Ledger = _ledger?.Capture(_settings.LedgerRetentionRuns) ?? new MetaRunLedgerSnapshot()
            });
        }
    }
}
