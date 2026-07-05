using System.Collections.Generic;

namespace Narrative.Threads.Core
{
    /// <summary>Default <see cref="IThreadLedger"/>: a pure-C# list in first-open order.</summary>
    public sealed class ThreadLedger : IThreadLedger
    {
        private readonly List<ThreadRecord> _threads = new List<ThreadRecord>();

        public IReadOnlyList<ThreadRecord> Threads => _threads;

        public int LiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _threads.Count; i++)
                {
                    if (_threads[i].State == ThreadState.Live)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool TryGet(string threadId, out ThreadRecord record)
        {
            for (int i = 0; i < _threads.Count; i++)
            {
                if (string.Equals(_threads[i].ThreadId, threadId, System.StringComparison.Ordinal))
                {
                    record = _threads[i];
                    return true;
                }
            }

            record = null;
            return false;
        }

        public ThreadRecord Open(string threadId, ThreadKind kind, int windowIndex)
        {
            if (string.IsNullOrEmpty(threadId))
            {
                return null;
            }

            if (TryGet(threadId, out var existing))
            {
                return existing;
            }

            var record = new ThreadRecord(threadId, kind, windowIndex);
            _threads.Add(record);
            return record;
        }

        public void NoteBeatResolved(string threadId)
        {
            if (!TryGet(threadId, out var record) || record.State != ThreadState.Live)
            {
                return;
            }

            record.Stage++;
            record.AdvancedSinceLastTick = true;
        }

        public void Resolve(string threadId)
        {
            if (!TryGet(threadId, out var record) || record.State != ThreadState.Live)
            {
                return;
            }

            record.State = ThreadState.Resolved;
            record.Reason = ThreadRetirementReason.None;
        }

        public void Fail(string threadId, ThreadRetirementReason reason)
        {
            if (!TryGet(threadId, out var record) || record.State != ThreadState.Live)
            {
                return;
            }

            record.State = ThreadState.Failed;
            record.Reason = reason;
        }

        public bool IsRetired(string threadId)
        {
            return TryGet(threadId, out var record) && record.State != ThreadState.Live;
        }

        public void Restore(IReadOnlyList<ThreadRecord> records)
        {
            _threads.Clear();
            if (records == null)
            {
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] != null)
                {
                    _threads.Add(records[i]);
                }
            }
        }
    }
}
