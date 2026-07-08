using System.Collections.Generic;
using Core.Persistence;

namespace MetaProgression.Core
{
    /// <summary>
    /// In-memory cross-run direction ledger (meta-progression FR8): accumulates the current run's
    /// installed part ids and consumed reagent artifact ids on top of the past runs loaded from the
    /// meta store. Recording is idempotent (sets, not lists); capture emits run-index-ordered
    /// entries with ordinal-sorted id lists pruned to the retention window, so identical play
    /// produces byte-identical saves (FR13).
    /// </summary>
    public sealed class RunLedger
    {
        private sealed class Entry
        {
            public readonly SortedSet<string> InstalledPartIds = new SortedSet<string>(System.StringComparer.Ordinal);
            public readonly SortedSet<string> SocketedArtifactIds = new SortedSet<string>(System.StringComparer.Ordinal);
        }

        private readonly SortedDictionary<int, Entry> _runs = new SortedDictionary<int, Entry>();

        public static RunLedger FromSnapshot(MetaRunLedgerSnapshot snapshot)
        {
            var ledger = new RunLedger();
            if (snapshot?.Runs == null)
            {
                return ledger;
            }

            foreach (var run in snapshot.Runs)
            {
                if (run == null)
                {
                    continue;
                }

                if (run.InstalledPartIds != null)
                {
                    foreach (var id in run.InstalledPartIds)
                    {
                        ledger.RecordInstalledPart(run.RunIndex, id);
                    }
                }

                if (run.SocketedArtifactIds != null)
                {
                    foreach (var id in run.SocketedArtifactIds)
                    {
                        ledger.RecordSocketedArtifact(run.RunIndex, id);
                    }
                }
            }

            return ledger;
        }

        public void RecordInstalledPart(int runIndex, string partId)
        {
            if (runIndex < 1 || string.IsNullOrEmpty(partId))
            {
                return;
            }

            EntryFor(runIndex).InstalledPartIds.Add(partId);
        }

        public void RecordSocketedArtifact(int runIndex, string artifactId)
        {
            if (runIndex < 1 || string.IsNullOrEmpty(artifactId))
            {
                return;
            }

            EntryFor(runIndex).SocketedArtifactIds.Add(artifactId);
        }

        /// <summary>
        /// The persistable image: entries ordered by run index, id lists ordinal-sorted, pruned to
        /// the last <paramref name="retentionRuns"/> recorded runs. A run with no recorded ids
        /// holds no entry — the direction window is defined over run indices, so an absent run
        /// simply contributes nothing.
        /// </summary>
        public MetaRunLedgerSnapshot Capture(int retentionRuns)
        {
            var snapshot = new MetaRunLedgerSnapshot();
            if (retentionRuns < 1)
            {
                return snapshot;
            }

            int skip = _runs.Count > retentionRuns ? _runs.Count - retentionRuns : 0;
            int index = 0;
            foreach (var pair in _runs)
            {
                if (index++ < skip)
                {
                    continue;
                }

                snapshot.Runs.Add(new RunLedgerEntryDto
                {
                    RunIndex = pair.Key,
                    InstalledPartIds = new List<string>(pair.Value.InstalledPartIds),
                    SocketedArtifactIds = new List<string>(pair.Value.SocketedArtifactIds)
                });
            }

            return snapshot;
        }

        private Entry EntryFor(int runIndex)
        {
            if (!_runs.TryGetValue(runIndex, out var entry))
            {
                entry = new Entry();
                _runs.Add(runIndex, entry);
            }

            return entry;
        }
    }
}
