using System;
using Narrative.Runtime.Snapshots;

namespace Core.Persistence
{
    /// <summary>
    /// The cross-run world memory (FR9): the Meta-horizon facts, written to <c>meta.json</c>,
    /// surviving every death and every fresh run. Deliberately independent of
    /// <see cref="RunSaveSnapshot"/> so the two files are independently recoverable (FR13).
    /// </summary>
    [Serializable]
    public class MetaMemorySnapshot : IVersionedSnapshot
    {
        // Stays 1: JsonSaveFile quarantines on version mismatch, so additive fields (Ledger)
        // must not bump it — an old file simply deserializes them to their defaults.
        public const int CurrentVersion = 1;

        public int Version;
        public FactStoreSnapshot Facts = new FactStoreSnapshot();
        public MetaRunLedgerSnapshot Ledger = new MetaRunLedgerSnapshot();

        int IVersionedSnapshot.Version => Version;
    }
}
