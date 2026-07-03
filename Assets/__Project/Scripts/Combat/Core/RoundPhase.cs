namespace Combat.Core
{
    /// <summary>
    /// The three phases of a combat round (Plan → Act → Resolve).
    /// Orthogonal to CombatPhase, which tracks the overall combat lifecycle.
    /// </summary>
    public enum RoundPhase
    {
        /// <summary>
        /// Every enemy decides its action up front; the decisions are locked and revealed.
        /// </summary>
        EnemyPlan,

        /// <summary>
        /// The player queues and executes freely, seeing all enemy intents; player actions
        /// resolve live during this phase.
        /// </summary>
        PlayerAct,

        /// <summary>
        /// Enemy committed actions fire exactly as revealed — they whiff if the board
        /// changed, they never re-target.
        /// </summary>
        EnemyResolve
    }
}
