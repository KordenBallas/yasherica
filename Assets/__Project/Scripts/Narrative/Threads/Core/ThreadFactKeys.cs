namespace Narrative.Threads.Core
{
    /// <summary>The thread subsystem's contribution to the fact vocabulary.</summary>
    public static class ThreadFactKeys
    {
        /// <summary>Per-thread retirement indicator (FR4/FR5): <c>world.&lt;threadId&gt;.thread_retired</c>,
        /// String, written once when a thread retires without a payoff. The quest-log / thread
        /// readout can gate on it later; there is no closing story beat (FR6).</summary>
        public const string Retired = "thread_retired";

        public const string RetiredValueExpired = "expired";
        public const string RetiredValueConflict = "conflict";
    }
}
