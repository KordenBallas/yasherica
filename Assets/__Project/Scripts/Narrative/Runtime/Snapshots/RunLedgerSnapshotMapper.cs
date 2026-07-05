using System.Collections.Generic;
using Narrative.Stories.Core;
using Narrative.Threads.Core;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>
    /// Pure bridge between the run ledgers (thread lifecycle + placed/resolved stories, R8) and their
    /// serializable snapshots. Both ledgers keep planner-driven registration order, so capture/restore
    /// is order-preserving and replay-stable (FR12).
    /// </summary>
    public static class RunLedgerSnapshotMapper
    {
        public static List<ThreadRecordSnapshot> CaptureThreads(IThreadLedger ledger)
        {
            var snapshots = new List<ThreadRecordSnapshot>();
            if (ledger == null)
            {
                return snapshots;
            }

            var threads = ledger.Threads;
            for (int i = 0; i < threads.Count; i++)
            {
                var record = threads[i];
                snapshots.Add(new ThreadRecordSnapshot
                {
                    ThreadId = record.ThreadId,
                    Kind = (int)record.Kind,
                    State = (int)record.State,
                    Reason = (int)record.Reason,
                    Stage = record.Stage,
                    WindowOpened = record.WindowOpened,
                    WindowsWithoutAdvance = record.WindowsWithoutAdvance,
                    AdvancedSinceLastTick = record.AdvancedSinceLastTick
                });
            }

            return snapshots;
        }

        public static void RestoreThreads(List<ThreadRecordSnapshot> snapshots, IThreadLedger ledger)
        {
            if (ledger == null)
            {
                return;
            }

            var records = new List<ThreadRecord>();
            if (snapshots != null)
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    var dto = snapshots[i];
                    records.Add(new ThreadRecord(dto.ThreadId, (ThreadKind)dto.Kind, dto.WindowOpened,
                        (ThreadState)dto.State, (ThreadRetirementReason)dto.Reason, dto.Stage,
                        dto.WindowsWithoutAdvance, dto.AdvancedSinceLastTick));
                }
            }

            ledger.Restore(records);
        }

        public static List<StoryRunEntrySnapshot> CaptureStories(IStoryRunLedger ledger)
        {
            var snapshots = new List<StoryRunEntrySnapshot>();
            if (ledger == null)
            {
                return snapshots;
            }

            var entries = ledger.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                snapshots.Add(new StoryRunEntrySnapshot
                {
                    StoryId = entries[i].StoryId,
                    ThreadId = entries[i].ThreadId,
                    Status = (int)entries[i].Status,
                    WindowPlaced = entries[i].WindowPlaced
                });
            }

            return snapshots;
        }

        public static void RestoreStories(List<StoryRunEntrySnapshot> snapshots, IStoryRunLedger ledger)
        {
            if (ledger == null)
            {
                return;
            }

            var entries = new List<StoryRunEntry>();
            if (snapshots != null)
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    var dto = snapshots[i];
                    entries.Add(new StoryRunEntry(dto.StoryId, dto.ThreadId,
                        (StoryRunStatus)dto.Status, dto.WindowPlaced));
                }
            }

            ledger.Restore(entries);
        }
    }
}
