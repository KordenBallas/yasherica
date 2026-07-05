namespace Narrative.Threads.Core
{
    /// <summary>Lifecycle state of a managed thread (R8/FR1).</summary>
    public enum ThreadState
    {
        Live = 0,

        /// <summary>Reached its authored payoff (resolution conditions satisfied).</summary>
        Resolved = 1,

        /// <summary>Retired without a payoff — expired (ephemeral timeout) or failed on a
        /// premise-fact conflict. The distinction lives in <see cref="ThreadRetirementReason"/>.</summary>
        Failed = 2
    }
}
