namespace Narrative.Threads.Core
{
    /// <summary>
    /// Mutable run-state of one managed thread (FR1): its lifecycle state, how far it advanced
    /// (stage = beats resolved), and the expiry clock (windows without an advance). Owned and
    /// mutated only by the <see cref="IThreadLedger"/> / maintenance tick so the lifecycle stays
    /// deterministic and replayable.
    /// </summary>
    public sealed class ThreadRecord
    {
        public string ThreadId { get; }
        public ThreadKind Kind { get; }
        public ThreadState State { get; internal set; }
        public ThreadRetirementReason Reason { get; internal set; }

        /// <summary>Beats of this thread the player has resolved (FR1: the director advances a
        /// thread's stage as its beats resolve). Placement alone does not advance it.</summary>
        public int Stage { get; internal set; }

        public int WindowOpened { get; }

        /// <summary>Whole planning windows since the last advance — the expiry clock (FR4).</summary>
        public int WindowsWithoutAdvance { get; internal set; }

        /// <summary>Set when a beat resolves between planning ticks; folded into the counters (and
        /// cleared) at the start of the next tick.</summary>
        public bool AdvancedSinceLastTick { get; internal set; }

        public ThreadRecord(string threadId, ThreadKind kind, int windowOpened)
        {
            ThreadId = threadId ?? string.Empty;
            Kind = kind;
            State = ThreadState.Live;
            Reason = ThreadRetirementReason.None;
            WindowOpened = windowOpened;
        }

        /// <summary>Full-state constructor for snapshot restore (same-assembly only).</summary>
        internal ThreadRecord(string threadId, ThreadKind kind, int windowOpened, ThreadState state,
            ThreadRetirementReason reason, int stage, int windowsWithoutAdvance, bool advancedSinceLastTick)
            : this(threadId, kind, windowOpened)
        {
            State = state;
            Reason = reason;
            Stage = stage;
            WindowsWithoutAdvance = windowsWithoutAdvance;
            AdvancedSinceLastTick = advancedSinceLastTick;
        }
    }
}
