using Combat.Core;

namespace Combat.Controller
{
    /// <summary>
    /// The per-mode strategy that supplies the three axes on which PvE and Arena combat diverge —
    /// round-lead / interleave, resolution order + commit source, and the win-condition set — while the
    /// shared round/state mechanics live in <see cref="CombatRoundEngine"/> (A1 reconvergence). The engine
    /// drives the flow at each divergence point and exposes the primitives the flow calls back into, so
    /// the two controllers no longer duplicate the round loop.
    /// </summary>
    public interface ICombatRoundFlow
    {
        /// <summary>
        /// Post-<see cref="CombatRoundEngine.Initialize"/> hook. Arena activates the match host (host role)
        /// and subscribes to the transport; PvE does nothing.
        /// </summary>
        void OnInitialized(CombatRoundEngine engine);

        /// <summary>
        /// Opens a round. PvE plans and reveals enemy intents locally and hands the opening lead to the
        /// initiator (D2); Arena opens the host gather and the hidden planning phase.
        /// </summary>
        void OnRoundStart(CombatRoundEngine engine);

        /// <summary>
        /// Processes a player action. PvE validates and executes locally; Arena intercepts terminal actions
        /// into an <c>ArenaCommit</c> sent to the host instead of executing them.
        /// </summary>
        Execution.ActionResult ProcessAction(CombatRoundEngine engine, IAction action);

        /// <summary>
        /// One committed intent just resolved — called after the state mutation, before the engine's win
        /// check. PvE does nothing; Arena's step-batching mode (P4-3b) gates the win condition on batch
        /// boundaries here so a mutual lethal exchange inside one batch kills both (a real draw).
        /// </summary>
        void OnIntentResolved(CombatRoundEngine engine, Core.EnemyIntent intent);

        /// <summary>
        /// The Resolve phase has fired every committed intent. PvE may re-open the player's Act phase for an
        /// enemy-led opening round; Arena always ends the round.
        /// </summary>
        void OnEnemyResolveFinished(CombatRoundEngine engine);

        /// <summary>
        /// Runs after the round-lifecycle tick and before the next round opens. Arena computes and publishes
        /// the lockstep <c>ArenaStateHash</c> (R10) here; PvE does nothing.
        /// </summary>
        void OnRoundEndTail(CombatRoundEngine engine);

        /// <summary>
        /// Cleanup on battlefield teardown. Arena unsubscribes the transport and deactivates the host; PvE
        /// does nothing.
        /// </summary>
        void OnCleanup(CombatRoundEngine engine);
    }
}
