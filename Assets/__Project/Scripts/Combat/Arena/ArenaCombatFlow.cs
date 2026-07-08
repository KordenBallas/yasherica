using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Controller;
using Combat.Core;
using Combat.Execution;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>
    /// The Arena round shape over the shared <see cref="CombatRoundEngine"/>: the symmetric lockstep round.
    /// Every player plans hidden with the normal planning UI (non-terminal actions run locally; TERMINAL
    /// actions are intercepted into an <see cref="ArenaCommit"/> instead of executing), the host gathers all
    /// alive players' commits, and the broadcast bundle is normalized + ordered identically on every client,
    /// then resolved through the shared PvE committed-intent resolver (whiff / fizzle / skip-dead unchanged).
    /// Phase mapping onto the shared <see cref="RoundPhase"/> values the UI consumes: PlayerAct = planning
    /// (hidden), EnemyResolve = the simultaneous resolution.
    /// </summary>
    public class ArenaCombatFlow : ICombatRoundFlow
    {
        private readonly ArenaCommitBuilder _commitBuilder;
        private readonly IArenaResolutionOrder _resolutionOrder;
        private readonly LastHeroStandingWinCondition _winCondition;
        private readonly IArenaTransport _transport;
        private readonly ArenaMatchHost _matchHost;
        private readonly IGameLogger _logger;

        private CombatRoundEngine _engine;
        private ulong _lastRoundHash;

        // The immutable state as the current round's planning opened (intents stripped) — the X1
        // resync anchor: snapshots for rejoiners are captured from it, and a host-migration
        // rollback re-opens the round on it. Retained on EVERY client, because migration can
        // promote anyone.
        private ICombatState _roundStartState;

        // Offline play always hosts; the networked entrypoint sets the role from the session before
        // Initialize — joined clients never assemble rounds.
        private bool _isHost = true;

        // P4-3b step batching (config-gated, default off): the batch plan for the round being
        // resolved + how many of its intents have fired.
        private ArenaBatchEligibility _batchEligibility;
        private IReadOnlyList<int> _batchEndIndices;
        private int _resolvedInBatchPlan;

        public ArenaCombatFlow(
            ArenaCommitBuilder commitBuilder,
            IArenaResolutionOrder resolutionOrder,
            LastHeroStandingWinCondition winCondition,
            IArenaTransport transport,
            ArenaMatchHost matchHost,
            IGameLogger logger)
        {
            _commitBuilder = commitBuilder;
            _resolutionOrder = resolutionOrder;
            _winCondition = winCondition;
            _transport = transport;
            _matchHost = matchHost;
            _logger = logger;
        }

        /// <summary>The current round's end-of-round lockstep hash (R10); 0 before round 1 ends.</summary>
        public ulong LastRoundHash => _lastRoundHash;

        /// <summary>The state as the current round's planning opened — the X1 resync anchor.</summary>
        public ICombatState RoundStartState => _roundStartState;

        /// <summary>
        /// Digests the round-start anchor for a rejoining/resyncing peer (X1). Always a planning
        /// boundary — never mid-resolve.
        /// </summary>
        public ArenaStateSnapshot CaptureRoundStartSnapshot()
        {
            return ArenaStateSnapshot.Capture(_roundStartState ?? _engine.State, _lastRoundHash);
        }

        /// <summary>
        /// Adopts an authoritative restored state (X1 rejoin / desync heal / migration rollback):
        /// the local sim, its resync anchor, and the lockstep hash all become the transferred
        /// truth; any local planning in flight is discarded by design.
        /// </summary>
        public void AdoptState(ICombatState restored, ulong lastRoundHash)
        {
            // A transfer always lands on a planning boundary — any half-resolved batch plan
            // (mid-batch migration/heal) is abandoned with the round it belonged to.
            ClearBatchPlan();

            _engine.SetState(restored);
            _roundStartState = restored;
            _lastRoundHash = lastRoundHash;

            if (_isHost)
            {
                _matchHost.SetExpectedHash(lastRoundHash);
            }

            _engine.RaiseStateChanged();
        }

        /// <summary>
        /// Feeds a bundle that arrived OUTSIDE the transport broadcast — the rejoin package
        /// replays the current round's bundle when it was assembled before the rejoiner attached.
        /// </summary>
        public void ReplayBundle(ArenaRoundBundle bundle)
        {
            HandleBundleReceived(bundle);
        }

        /// <summary>
        /// This machine won the host election (X1 migration): it assembles rounds from now on.
        /// The caller re-seeds the match host's seat book and then rolls the round back via
        /// <see cref="ReopenCurrentRound"/>.
        /// </summary>
        public void PromoteToHost()
        {
            _isHost = true;
            _matchHost.Activate();
        }

        /// <summary>
        /// Arms P4-3b step batching: every commit's k-th steps resolve as one simultaneous batch
        /// (death checked only at batch boundaries — a mutual lethal exchange kills both). Called
        /// by the entrypoint when the config gate is on; never in PvE.
        /// </summary>
        public void EnableStepBatching(ArenaBatchEligibility batchEligibility)
        {
            _batchEligibility = batchEligibility;
        }

        /// <summary>
        /// Rolls the current round back to its planning start (X1 migration: in-flight commits on
        /// the dead host are lost, so every client re-plans round N on the identical anchor). On
        /// the promoted host this also re-opens the gather.
        /// </summary>
        public void ReopenCurrentRound()
        {
            ClearBatchPlan();

            if (_roundStartState != null)
            {
                _engine.SetState(_roundStartState);
            }

            if (_isHost)
            {
                _matchHost.BeginRound(_engine.State.TurnNumber, AlivePlayerIds(_engine));
            }

            _engine.BeginPlayerAct();
        }

        /// <summary>
        /// Sets whether this machine assembles rounds (the session host). Must be called before
        /// <see cref="CombatRoundEngine.Initialize"/> — joined clients never activate the match host.
        /// </summary>
        public void SetHostRole(bool isHost)
        {
            _isHost = isHost;
        }

        /// <summary>
        /// Arms the win condition once every roster slot's hero has been added — heroes spawn one by one and
        /// the first must not "win" a board that is still filling up.
        /// </summary>
        public void ArmWinCondition()
        {
            _winCondition.Arm();
        }

        public void OnInitialized(CombatRoundEngine engine)
        {
            _engine = engine;

            if (_isHost)
            {
                _matchHost.Activate();
            }

            _transport.BundleReceived += HandleBundleReceived;
        }

        /// <summary>
        /// Planning opens: the host starts gathering BEFORE the phase event fires so sources that submit
        /// synchronously on the phase change (offline AI dummies) land in an open round.
        /// </summary>
        public void OnRoundStart(CombatRoundEngine engine)
        {
            // The retained copy drops the previous round's intents — they belong to the resolve
            // phase that already happened (the live state is immutable; this mutates nothing).
            _roundStartState = (engine.State as CombatState)
                .WithEnemyIntents(new List<EnemyIntent>());

            if (_isHost)
            {
                _matchHost.BeginRound(engine.State.TurnNumber, AlivePlayerIds(engine));
            }

            engine.BeginPlayerAct();
        }

        public ActionResult ProcessAction(CombatRoundEngine engine, IAction action)
        {
            var validationResult = engine.Validator.ValidateDetailed(engine.State, action);
            if (!validationResult.IsValid)
            {
                return ActionResult.Failed(engine.State, validationResult.FailureReason);
            }

            if (!action.EndsTurn)
            {
                // Free planning feedback (reorder / turn) runs locally exactly like PvE; facing becomes
                // canonical from the commit's FinalFacing at normalization.
                var result = engine.Executor.ExecuteWithResult(engine.State, action);
                engine.SetState(result.NewState);
                engine.RaiseStateChanged();
                return result;
            }

            if (action is ScheduleAbilityAction || action is EndUnitTurnAction)
            {
                // Terminal but board-effect-free: growing the ability queue is local-only planning state no
                // other client ever reads (the volley ships as committed steps when it is executed), so it
                // runs through the normal executor — which also sets the acted flag — and locks the round in
                // with an empty commitment.
                var result = engine.Executor.ExecuteWithResult(engine.State, action);
                engine.SetState(result.NewState);
                engine.RaiseStateChanged();

                var actedUnit = engine.State.GetUnit(action.UnitId);
                SubmitCommit(engine, _commitBuilder.FromTerminalAction(engine.State, actedUnit, action));
                return result;
            }

            // Move / ExecuteQueue → intercept: never execute locally. The whole commitment is snapshotted
            // (committed cells frozen from position + facing) and sent to the host; it fires only when the
            // bundle resolves — on every client identically.
            var unit = engine.State.GetUnit(action.UnitId);
            var commit = _commitBuilder.FromTerminalAction(engine.State, unit, action);

            engine.SetState((engine.State as CombatState).WithUpdatedUnit((unit as Unit).WithActedThisTurn(true)));
            engine.RaiseStateChanged();

            SubmitCommit(engine, commit);
            return ActionResult.Successful(engine.State);
        }

        /// <summary>
        /// P4-3b batch bookkeeping: the win check sleeps inside a batch and wakes at every batch
        /// boundary (the engine checks right after this hook), where the next batch's eligibility
        /// snapshot is also taken. No-op in sequential mode.
        /// </summary>
        public void OnIntentResolved(CombatRoundEngine engine, EnemyIntent intent)
        {
            if (_batchEndIndices == null)
                return;

            _resolvedInBatchPlan++;
            if (_batchEndIndices.Contains(_resolvedInBatchPlan))
            {
                _winCondition.Resume();
                _batchEligibility.RefreshFrom(engine.State);
            }
            else
            {
                _winCondition.Suspend();
            }
        }

        public void OnEnemyResolveFinished(CombatRoundEngine engine)
        {
            ClearBatchPlan();
            engine.EndRound();
        }

        private void ClearBatchPlan()
        {
            if (_batchEndIndices == null)
                return;

            _batchEndIndices = null;
            _resolvedInBatchPlan = 0;
            _batchEligibility.EndBatchMode();
            _winCondition.Resume();
        }

        public void OnRoundEndTail(CombatRoundEngine engine)
        {
            _lastRoundHash = ArenaStateHash.Compute(engine.State);
            _logger.Info(LogCategory.Combat,
                $"[ArenaCombatFlow] Round {engine.State.TurnNumber - 1} complete — ArenaStateHash {_lastRoundHash:X16}");

            if (_isHost)
            {
                _matchHost.SetExpectedHash(_lastRoundHash);
            }
        }

        public void OnCleanup(CombatRoundEngine engine)
        {
            _transport.BundleReceived -= HandleBundleReceived;
            if (_isHost)
            {
                _matchHost.Deactivate();
            }
        }

        private void SubmitCommit(CombatRoundEngine engine, ArenaCommit commit)
        {
            _logger.Info(LogCategory.Combat,
                $"[ArenaCombatFlow] Player {commit.PlayerId} locked in ({commit.Steps.Count} step(s)) for round {engine.State.TurnNumber}");
            _transport.SubmitCommit(new ArenaCommitEnvelope(engine.State.TurnNumber, commit, _lastRoundHash));
        }

        private void HandleBundleReceived(ArenaRoundBundle bundle)
        {
            if (bundle.RoundNumber != _engine.State.TurnNumber)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaCombatFlow] Ignoring bundle for round {bundle.RoundNumber} (current round {_engine.State.TurnNumber})");
                return;
            }

            NormalizeFromBundle(bundle);

            if (_batchEligibility != null)
            {
                // P4-3b: step-major plan, win check gated to batch boundaries, eligibility
                // snapshotted per batch (a mid-batch death does not cancel same-batch peers).
                var batched = ArenaStepBatcher.Batch(bundle, _resolutionOrder);
                _batchEndIndices = batched.BatchEndIndices;
                _resolvedInBatchPlan = 0;
                _batchEligibility.BeginBatchMode();
                _batchEligibility.RefreshFrom(_engine.State);
                if (batched.Intents.Count > 0)
                {
                    _winCondition.Suspend();
                }

                _engine.EnterEnemyResolveWithIntents(batched.Intents);
                return;
            }

            var intents = _resolutionOrder.Order(bundle);
            _engine.EnterEnemyResolveWithIntents(intents);
        }

        /// <summary>
        /// Makes the pre-resolution board canonical on every client: departed players' units die (R9 — the
        /// departure rides the bundle, never local timing), every commit's lock-time facing is applied
        /// (converging remote units that never saw the owner's aiming), and a unit whose commit fires its
        /// volley gets its queue cleared (the content now lives in the committed steps). Un-executed queues
        /// stay local planning state — no other client reads them, so they cannot diverge the sim.
        /// </summary>
        private void NormalizeFromBundle(ArenaRoundBundle bundle)
        {
            var state = _engine.State;

            foreach (var departedPlayerId in bundle.DepartedPlayerIds)
            {
                foreach (var unit in state.Units.Where(u => u.Owner.Id == departedPlayerId).ToList())
                {
                    state = (state as CombatState).WithUpdatedUnit((unit as Unit).WithHP(0));
                    _logger.Info(LogCategory.Combat,
                        $"[ArenaCombatFlow] Player {departedPlayerId} departed — unit {unit.Id} removed from the fight");
                }
            }

            foreach (var commit in bundle.Commits)
            {
                var unit = state.GetUnit(commit.UnitId) as Unit;
                if (unit == null)
                    continue;

                if (commit.Steps.Any(s => s.IsAbility))
                {
                    unit = unit.WithAbilityQueue(new List<ScheduledAbility>());
                }

                unit = unit.WithFacingDirection(commit.FinalFacing);
                state = (state as CombatState).WithUpdatedUnit(unit);
            }

            _engine.SetState(state);
        }

        private IReadOnlyList<int> AlivePlayerIds(CombatRoundEngine engine)
        {
            return engine.State.Players
                .Where(p => engine.State.GetUnitsByPlayer(p).Any(u => u.IsAlive))
                .Select(p => p.Id)
                .OrderBy(id => id)
                .ToList();
        }
    }
}
