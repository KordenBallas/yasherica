using System;
using System.Collections.Generic;

namespace Core.Persistence
{
    /// <summary>One past run's direction-relevant record (primitive fields, JsonUtility-friendly).</summary>
    [Serializable]
    public class RunLedgerEntryDto
    {
        public int RunIndex;
        public List<string> InstalledPartIds = new List<string>();
        public List<string> SocketedArtifactIds = new List<string>();
    }

    /// <summary>
    /// The cross-run direction ledger (meta-progression FR8): which parts the hero installed and
    /// which artifact reagents were socketed, per run, for the last few runs. Raw ids only — race
    /// markers and trait families are resolved at read time from the content catalogs, so the
    /// ledger survives content edits. Lives inside <see cref="MetaMemorySnapshot"/> as an additive
    /// field: an older <c>meta.json</c> without it deserializes to an empty ledger (a neutral
    /// direction), never a quarantine.
    /// </summary>
    [Serializable]
    public class MetaRunLedgerSnapshot
    {
        public List<RunLedgerEntryDto> Runs = new List<RunLedgerEntryDto>();
    }
}
