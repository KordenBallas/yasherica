using Core.Logging;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using Zenject;

namespace Core.Persistence
{
    /// <summary>
    /// Loads the cross-run memory into the unified fact store at Area boot, fresh run and continue
    /// alike (FR9): the world starts every run already remembering. Runs at default initialization
    /// order, i.e. AFTER the run-restore coordinator — on a continue the always-on file wins over
    /// the run image's Meta section (D9).
    /// </summary>
    public sealed class MetaMemoryBootstrap : IInitializable
    {
        private readonly IMetaMemoryStore _store;
        private readonly IFactStore _facts;
        private readonly IGameLogger _logger;

        public MetaMemoryBootstrap(IMetaMemoryStore store, IFactStore facts, IGameLogger logger = null)
        {
            _store = store;
            _facts = facts;
            _logger = logger;
        }

        public void Initialize()
        {
            var memory = _store.LoadOrEmpty();
            FactStoreSnapshotMapper.Restore(memory.Facts, _facts);
            if (memory.Facts.Entries.Count > 0)
            {
                _logger?.Info(LogCategory.Persistence,
                    $"[MetaMemoryBootstrap] World memory loaded: {memory.Facts.Entries.Count} meta fact(s).");
            }
        }
    }
}
