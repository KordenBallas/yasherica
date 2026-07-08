using System.Collections.Generic;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Execution;
using Combat.TurnManagement;
using Core.Logging;
using UnityEngine;

namespace Combat.Arena
{
    /// <summary>
    /// The Arena combat controller: a thin <see cref="ICombatController"/> adapter over the shared
    /// <see cref="CombatRoundEngine"/> configured with the Arena round shape (<see cref="ArenaCombatFlow"/>)
    /// and the last-hero-standing win condition. The whole PvE presentation stack (action panel, overhead
    /// plan icons, ghost telegraph, resolve pacing) works against the unchanged seam untouched. Round/state
    /// mechanics are single-sourced in the engine (A1 reconvergence — this controller was a verbatim copy of
    /// the PvE one); this class composes the engine + Arena flow, forwards the seam, and keeps the Arena-only
    /// surface (<see cref="SetHostRole"/>, <see cref="ArmWinCondition"/>, <see cref="LastRoundHash"/>) that
    /// <see cref="View.ArenaSceneEntrypoint"/> drives.
    /// </summary>
    public class ArenaCombatController : ICombatController, IArenaCanonicalStateSource
    {
        private readonly CombatRoundEngine _engine;
        private readonly ArenaCombatFlow _flow;

        public ArenaCombatController(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            RoundLifecycleProcessor roundLifecycle,
            EnemyIntentResolver intentResolver,
            ArenaCommitBuilder commitBuilder,
            IArenaResolutionOrder resolutionOrder,
            LastHeroStandingWinCondition winCondition,
            IArenaTransport transport,
            ArenaMatchHost matchHost,
            BattlefieldFactory battlefieldFactory,
            HexDirectionConfig hexConfig,
            IGameLogger logger)
        {
            _flow = new ArenaCombatFlow(commitBuilder, resolutionOrder, winCondition, transport, matchHost, logger);
            _engine = new CombatRoundEngine(
                actionValidator,
                actionExecutor,
                turnManager,
                intentResolver,
                roundLifecycle,
                battlefieldFactory,
                hexConfig,
                new IWinCondition[] { winCondition },
                _flow,
                logger);
        }

        public ICombatState CombatState => _engine.State;
        public ITurnManager TurnManager => _engine.TurnManager;
        public IBattlefield Battlefield => _engine.Battlefield;

        // Arena resolves by its own IArenaResolutionOrder (RotatingInitiativeOrder), not the PvE
        // initiator-leads rule — the D2 opening-initiator concept does not apply here.
        public CombatInitiator OpeningInitiator => CombatInitiator.Player;

        /// <summary>The current round's end-of-round lockstep hash (R10); 0 before round 1 ends.</summary>
        public ulong LastRoundHash => _flow.LastRoundHash;

        public event System.Action<ICombatState> OnStateChanged
        {
            add => _engine.OnStateChanged += value;
            remove => _engine.OnStateChanged -= value;
        }
        public event System.Action<IPlayer> OnTurnStarted
        {
            add => _engine.OnTurnStarted += value;
            remove => _engine.OnTurnStarted -= value;
        }
        public event System.Action<RoundPhase> OnRoundPhaseChanged
        {
            add => _engine.OnRoundPhaseChanged += value;
            remove => _engine.OnRoundPhaseChanged -= value;
        }
        public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed
        {
            add => _engine.OnEnemyPlansRevealed += value;
            remove => _engine.OnEnemyPlansRevealed -= value;
        }
        public event System.Action<IPlayer, CombatPhase> OnGameEnded
        {
            add => _engine.OnGameEnded += value;
            remove => _engine.OnGameEnded -= value;
        }

        /// <summary>
        /// Sets whether this machine assembles rounds (the session host). Must be called before
        /// <see cref="Initialize"/> — joined clients never activate the match host.
        /// </summary>
        public void SetHostRole(bool isHost) => _flow.SetHostRole(isHost);

        /// <summary>
        /// Arms the win condition once every roster slot's hero has been added — heroes spawn one by one and
        /// the first must not "win" a board that is still filling up.
        /// </summary>
        public void ArmWinCondition() => _flow.ArmWinCondition();

        // --- X1 reconnect / resync / migration surface (forwards the Arena flow) ---

        /// <summary>The state as the current round's planning opened — the X1 resync anchor.</summary>
        public ICombatState RoundStartState => _flow.RoundStartState;

        /// <summary>Digests the round-start anchor for a rejoining/resyncing peer (X1).</summary>
        public ArenaStateSnapshot CaptureRoundStartSnapshot() => _flow.CaptureRoundStartSnapshot();

        /// <summary>Adopts an authoritative restored state (X1 rejoin / desync heal / migration).</summary>
        public void AdoptState(ICombatState restored, ulong lastRoundHash)
            => _flow.AdoptState(restored, lastRoundHash);

        /// <summary>Feeds the rejoin package's replayed bundle (X1).</summary>
        public void ReplayBundle(ArenaRoundBundle bundle) => _flow.ReplayBundle(bundle);

        /// <summary>This machine won the host election (X1 migration).</summary>
        public void PromoteToHost() => _flow.PromoteToHost();

        /// <summary>Rolls the current round back to its planning start (X1 migration).</summary>
        public void ReopenCurrentRound() => _flow.ReopenCurrentRound();

        /// <summary>Arms P4-3b per-step damage batching (config-gated; default off).</summary>
        public void EnableStepBatching(ArenaBatchEligibility batchEligibility)
            => _flow.EnableStepBatching(batchEligibility);

        // openingInitiator is ignored: Arena orders by IArenaResolutionOrder, not initiator-leads.
        public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players,
            CombatInitiator openingInitiator = CombatInitiator.Enemy)
            => _engine.Initialize(initialState, players, openingInitiator);

        public void AddUnit(IUnit unit) => _engine.AddUnit(unit);

        public void BeginRounds() => _engine.BeginRounds();

        public bool ResolveNextEnemyIntent() => _engine.ResolveNextEnemyIntent();

        public ActionResult ProcessAction(IAction action) => _engine.ProcessAction(action);

        public void Update() => _engine.Update();

        public void InitializeBattlefield(PlatformHexSurface surface, Vector3 center)
            => _engine.InitializeBattlefield(surface, center);

        public void CleanupBattlefield() => _engine.CleanupBattlefield();
    }
}
