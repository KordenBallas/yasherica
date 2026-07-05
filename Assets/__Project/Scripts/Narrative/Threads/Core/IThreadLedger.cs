using System.Collections.Generic;

namespace Narrative.Threads.Core
{
    /// <summary>
    /// Run-scoped registry of managed threads (R8/FR1) — the live counterpart of the thread
    /// vocabulary, holding each thread's lifecycle across streamed windows. Registration order is
    /// driven by the seeded planner, so iterating <see cref="Threads"/> is replay-stable (the
    /// <c>LiveQuestRegistry</c> pattern). State transitions come from two writers only: the planner
    /// (<see cref="Open"/> at placement), and the resolution relay / maintenance tick
    /// (<see cref="NoteBeatResolved"/>, <see cref="Resolve"/>, <see cref="Fail"/>).
    /// </summary>
    public interface IThreadLedger
    {
        IReadOnlyList<ThreadRecord> Threads { get; }

        /// <summary>Threads currently in <see cref="ThreadState.Live"/> — the concurrency-ceiling
        /// count (FR7).</summary>
        int LiveCount { get; }

        bool TryGet(string threadId, out ThreadRecord record);

        /// <summary>Opens a thread on first placement; idempotent — re-opening an already-tracked
        /// thread (live or retired) returns the existing record unchanged.</summary>
        ThreadRecord Open(string threadId, ThreadKind kind, int windowIndex);

        /// <summary>A beat of this thread resolved (player engaged): stage++ and the expiry clock
        /// rearms at the next planning tick. Ignored for unknown or retired threads.</summary>
        void NoteBeatResolved(string threadId);

        void Resolve(string threadId);

        void Fail(string threadId, ThreadRetirementReason reason);

        /// <summary>True when the thread ended (resolved or failed) — its beats must never be
        /// placed again (FR9).</summary>
        bool IsRetired(string threadId);

        /// <summary>Replaces the ledger's content from a snapshot (save/load restore, FR12).
        /// Restored order is the captured registration order, so replay stays stable.</summary>
        void Restore(IReadOnlyList<ThreadRecord> records);
    }
}
