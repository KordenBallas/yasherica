using System.Collections.Generic;
using System.Linq;
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
    /// The Arena round loop behind the unchanged <see cref="ICombatController"/> seam — the whole
    /// PvE presentation stack (action panel, overhead plan icons, ghost telegraph, resolve pacing)
    /// works against it untouched. The symmetric round: every player plans hidden with the normal
    /// planning UI (non-terminal actions run locally; TERMINAL actions are intercepted into an
    /// <see cref="ArenaCommit"/> instead of executing), the host gathers all alive players' commits,
    /// and the broadcast bundle is normalized + resolved identically on every client through the
    /// PvE committed-intent resolver (whiff / fizzle / skip-dead semantics unchanged).
    /// Phase mapping onto the PvE <see cref="RoundPhase"/> values the UI consumes:
    /// PlayerAct = planning (hidden), EnemyResolve = the simultaneous resolution.
    /// </summary>
    public class ArenaCombatController : ICombatController
    {
        private readonly IActionValidator _actionValidator;
        private readonly IActionExecutor _actionExecutor;
        private readonly ITurnManager _turnManager;
        private readonly RoundLifecycleProcessor _roundLifecycle;
        private readonly EnemyIntentResolver _intentResolver;
        private readonly ArenaCommitBuilder _commitBuilder;
        private readonly IArenaResolutionOrder _resolutionOrder;
        private readonly LastHeroStandingWinCondition _winCondition;
        private readonly IArenaTransport _transport;
        private readonly ArenaMatchHost _matchHost;
        private readonly BattlefieldFactory _battlefieldFactory;
        private readonly HexDirectionConfig _hexConfig;
        private readonly IGameLogger _logger;

        private ICombatState _gameState;
        private IBattlefield _battlefield;
        private int _resolvedIntentCount;
        private ulong _lastRoundHash;

        // Offline play always hosts; the networked entrypoint sets the role from the session
        // before Initialize — joined clients never assemble rounds.
        private bool _isHost = true;

        public ICombatState CombatState => _gameState;
        public ITurnManager TurnManager => _turnManager;
        public IBattlefield Battlefield => _battlefield;

        public event System.Action<ICombatState> OnStateChanged;
        public event System.Action<IPlayer> OnTurnStarted;
        public event System.Action<RoundPhase> OnRoundPhaseChanged;
        public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed;
        public event System.Action<IPlayer, CombatPhase> OnGameEnded;

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
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnManager = turnManager;
            _roundLifecycle = roundLifecycle;
            _intentResolver = intentResolver;
            _commitBuilder = commitBuilder;
            _resolutionOrder = resolutionOrder;
            _winCondition = winCondition;
            _transport = transport;
            _matchHost = matchHost;
            _battlefieldFactory = battlefieldFactory;
            _hexConfig = hexConfig;
            _logger = logger;
        }

        /// <summary>The current round's end-of-round lockstep hash (R10); 0 before round 1 ends.</summary>
        public ulong LastRoundHash => _lastRoundHash;

        /// <summary>
        /// Sets whether this machine assembles rounds (the session host). Must be called before
        /// <see cref="Initialize"/> — joined clients never activate the match host.
        /// </summary>
        public void SetHostRole(bool isHost)
        {
            _isHost = isHost;
        }

        public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players)
        {
            _gameState = initialState;

            if (_battlefield != null && _gameState is CombatState combatState)
            {
                _gameState = combatState.WithBattlefield(_battlefield);
            }

            _turnManager.Initialize(players);

            _gameState = (_gameState as CombatState).WithPhase(CombatPhase.Combat);
            _gameState = (_gameState as CombatState).WithCurrentPlayer(_turnManager.CurrentPlayer);

            if (_isHost)
            {
                _matchHost.Activate();
            }

            _transport.BundleReceived += HandleBundleReceived;

            OnStateChanged?.Invoke(_gameState);
        }

        /// <summary>
        /// Arms the win condition once every roster slot's hero has been added — heroes spawn one
        /// by one and the first must not "win" a board that is still filling up.
        /// </summary>
        public void ArmWinCondition()
        {
            _winCondition.Arm();
        }

        public void BeginRounds()
        {
            _logger.Info(LogCategory.Combat, "[ArenaCombatController] BeginRounds — starting the first round");
            StartRound();
        }

        public void AddUnit(IUnit unit)
        {
            if (unit == null)
            {
                _logger.Warning(LogCategory.Combat, "[ArenaCombatController] Cannot add null unit");
                return;
            }

            if (!(unit is Unit))
            {
                _logger.Error(LogCategory.Combat,
                    $"[ArenaCombatController] CombatState can only contain Unit instances. Received: {unit.GetType().Name}");
                return;
            }

            if (_gameState.GetUnit(unit.Id) != null)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaCombatController] Unit with ID {unit.Id} already exists. Skipping add.");
                return;
            }

            var newUnits = new List<IUnit>(_gameState.Units) { unit };
            _gameState = (_gameState as CombatState).WithUnits(newUnits);

            _logger.Info(LogCategory.Combat,
                $"[ArenaCombatController] Added unit {unit.Id} (Owner: {unit.Owner?.Name ?? "null"}, Position: {unit.Position})");
            OnStateChanged?.Invoke(_gameState);
        }

        public ActionResult ProcessAction(IAction action)
        {
            var validationResult = _actionValidator.ValidateDetailed(_gameState, action);
            if (!validationResult.IsValid)
            {
                return ActionResult.Failed(_gameState, validationResult.FailureReason);
            }

            if (!action.EndsTurn)
            {
                // Free planning feedback (reorder / turn) runs locally exactly like PvE; facing
                // becomes canonical from the commit's FinalFacing at normalization.
                var result = _actionExecutor.ExecuteWithResult(_gameState, action);
                _gameState = result.NewState;
                OnStateChanged?.Invoke(_gameState);
                return result;
            }

            if (action is ScheduleAbilityAction || action is EndUnitTurnAction)
            {
                // Terminal but board-effect-free: growing the ability queue is local-only planning
                // state no other client ever reads (the volley ships as committed steps when it is
                // executed), so it runs through the normal executor — which also sets the acted
                // flag — and locks the round in with an empty commitment.
                var result = _actionExecutor.ExecuteWithResult(_gameState, action);
                _gameState = result.NewState;
                OnStateChanged?.Invoke(_gameState);

                var actedUnit = _gameState.GetUnit(action.UnitId);
                SubmitCommit(_commitBuilder.FromTerminalAction(_gameState, actedUnit, action));
                return result;
            }

            // Move / ExecuteQueue → intercept: never execute locally. The whole commitment is
            // snapshotted (committed cells frozen from position + facing) and sent to the host;
            // it fires only when the bundle resolves — on every client identically.
            var unit = _gameState.GetUnit(action.UnitId);
            var commit = _commitBuilder.FromTerminalAction(_gameState, unit, action);

            _gameState = (_gameState as CombatState).WithUpdatedUnit((unit as Unit).WithActedThisTurn(true));
            OnStateChanged?.Invoke(_gameState);

            SubmitCommit(commit);
            return ActionResult.Successful(_gameState);
        }

        public void Update()
        {
            CheckWinConditions();
        }

        private void SubmitCommit(ArenaCommit commit)
        {
            _logger.Info(LogCategory.Combat,
                $"[ArenaCombatController] Player {commit.PlayerId} locked in ({commit.Steps.Count} step(s)) for round {_gameState.TurnNumber}");
            _transport.SubmitCommit(new ArenaCommitEnvelope(_gameState.TurnNumber, commit, _lastRoundHash));
        }

        public bool ResolveNextEnemyIntent()
        {
            if (_gameState.Phase != CombatPhase.Combat)
                return false;
            if (_gameState.RoundPhase != RoundPhase.EnemyResolve)
                return false;

            if (_resolvedIntentCount >= _gameState.EnemyIntents.Count)
            {
                EndRound();
                return false;
            }

            var intent = _gameState.EnemyIntents[_resolvedIntentCount];
            _resolvedIntentCount++;

            _logger.Info(LogCategory.Combat,
                $"[ArenaCombatController] Resolving intent {_resolvedIntentCount}/{_gameState.EnemyIntents.Count} (unit {intent.UnitId}, {intent.Action.Type})");
            _gameState = _intentResolver.Resolve(_gameState, intent);

            OnStateChanged?.Invoke(_gameState);
            CheckWinConditions();

            if (_gameState.Phase != CombatPhase.Combat)
                return false;

            if (_resolvedIntentCount >= _gameState.EnemyIntents.Count)
            {
                EndRound();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Planning opens: the host starts gathering BEFORE the phase event fires so sources that
        /// submit synchronously on the phase change (offline AI dummies) land in an open round.
        /// </summary>
        private void StartRound()
        {
            if (_gameState.Phase != CombatPhase.Combat)
                return;

            _resolvedIntentCount = 0;

            if (_isHost)
            {
                _matchHost.BeginRound(_gameState.TurnNumber, AlivePlayerIds());
            }

            SetRoundPhase(RoundPhase.PlayerAct);
            OnTurnStarted?.Invoke(_turnManager.CurrentPlayer);
            OnStateChanged?.Invoke(_gameState);
        }

        private void HandleBundleReceived(ArenaRoundBundle bundle)
        {
            if (bundle.RoundNumber != _gameState.TurnNumber)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaCombatController] Ignoring bundle for round {bundle.RoundNumber} (current round {_gameState.TurnNumber})");
                return;
            }

            NormalizeFromBundle(bundle);

            var intents = _resolutionOrder.Order(bundle);
            _gameState = (_gameState as CombatState).WithEnemyIntents(intents);

            OnEnemyPlansRevealed?.Invoke(intents);
            SetRoundPhase(RoundPhase.EnemyResolve);
            OnStateChanged?.Invoke(_gameState);
        }

        /// <summary>
        /// Makes the pre-resolution board canonical on every client: departed players' units die
        /// (R9 — the departure rides the bundle, never local timing), every commit's lock-time
        /// facing is applied (converging remote units that never saw the owner's aiming), and a
        /// unit whose commit fires its volley gets its queue cleared (the content now lives in
        /// the committed steps). Un-executed queues stay local planning state — no other client
        /// reads them, so they cannot diverge the sim (they are not part of the state hash).
        /// </summary>
        private void NormalizeFromBundle(ArenaRoundBundle bundle)
        {
            foreach (var departedPlayerId in bundle.DepartedPlayerIds)
            {
                foreach (var unit in _gameState.Units.Where(u => u.Owner.Id == departedPlayerId).ToList())
                {
                    _gameState = (_gameState as CombatState).WithUpdatedUnit((unit as Unit).WithHP(0));
                    _logger.Info(LogCategory.Combat,
                        $"[ArenaCombatController] Player {departedPlayerId} departed — unit {unit.Id} removed from the fight");
                }
            }

            foreach (var commit in bundle.Commits)
            {
                var unit = _gameState.GetUnit(commit.UnitId) as Unit;
                if (unit == null)
                    continue;

                if (commit.Steps.Any(s => s.IsAbility))
                {
                    unit = unit.WithAbilityQueue(new List<ScheduledAbility>());
                }

                unit = unit.WithFacingDirection(commit.FinalFacing);
                _gameState = (_gameState as CombatState).WithUpdatedUnit(unit);
            }
        }

        private void EndRound()
        {
            _gameState = _roundLifecycle.ApplyRoundEndEffects(_gameState);

            _turnManager.NextTurn();
            _gameState = (_gameState as CombatState).WithNextTurn();

            _gameState = _roundLifecycle.ResetUnitsForNewRound(_gameState);
            _gameState = _roundLifecycle.ApplyRoundStartEffects(_gameState);

            _lastRoundHash = ArenaStateHash.Compute(_gameState);
            _logger.Info(LogCategory.Combat,
                $"[ArenaCombatController] Round {_gameState.TurnNumber - 1} complete — ArenaStateHash {_lastRoundHash:X16}");

            if (_isHost)
            {
                _matchHost.SetExpectedHash(_lastRoundHash);
            }

            StartRound();
        }

        private void SetRoundPhase(RoundPhase phase)
        {
            _gameState = (_gameState as CombatState).WithRoundPhase(phase);
            _logger.Info(LogCategory.Combat, $"[ArenaCombatController] Round phase → {phase}");
            OnRoundPhaseChanged?.Invoke(phase);
        }

        private void CheckWinConditions()
        {
            if (_gameState.Phase != CombatPhase.Combat)
                return;

            if (_winCondition.Check(_gameState, out var winner))
            {
                var newPhase = winner != null ? CombatPhase.Victory : CombatPhase.Defeat;
                _gameState = (_gameState as CombatState).WithPhase(newPhase);

                _logger.Info(LogCategory.Combat,
                    winner != null
                        ? $"[ArenaCombatController] Match over — {winner.Name} is the last hero standing"
                        : "[ArenaCombatController] Match over — draw (no heroes left standing)");

                OnGameEnded?.Invoke(winner, newPhase);
                OnStateChanged?.Invoke(_gameState);
            }
        }

        private IReadOnlyList<int> AlivePlayerIds()
        {
            return _gameState.Players
                .Where(p => _gameState.GetUnitsByPlayer(p).Any(u => u.IsAlive))
                .Select(p => p.Id)
                .OrderBy(id => id)
                .ToList();
        }

        public void InitializeBattlefield(PlatformHexSurface surface, Vector3 center)
        {
            _battlefield = _battlefieldFactory.Create();
            _battlefield.Initialize(surface, center, _hexConfig);
            _battlefield.Activate();

            if (_gameState != null && _gameState is CombatState combatState)
            {
                _gameState = combatState.WithBattlefield(_battlefield);
                OnStateChanged?.Invoke(_gameState);
            }
        }

        public void CleanupBattlefield()
        {
            _transport.BundleReceived -= HandleBundleReceived;
            if (_isHost)
            {
                _matchHost.Deactivate();
            }

            _battlefield?.Clear();
            _battlefield = null;
        }
    }
}
