using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.TurnManagement;
using Core.Logging;
using UnityEngine;

namespace Combat.Controller
{
    /// <summary>
    /// The shared, pure-C# combat round/state core used by both the PvE <see cref="CombatController"/> and
    /// the <see cref="Combat.Arena.ArenaCombatController"/> (A1 reconvergence — the two controllers were
    /// verbatim copies). It owns the unit list, battlefield lifecycle, phase sequencing, the resolve loop,
    /// the end-of-round lifecycle, win checks, and the five combat events. Everything that genuinely differs
    /// between PvE and Arena — round-lead / interleave, resolution order + commit source, and the win-condition
    /// set — is supplied by an injected <see cref="ICombatRoundFlow"/> the engine drives at each divergence
    /// point. The engine carries <b>zero</b> Arena dependencies; the Arena flow depends inward on the engine.
    /// </summary>
    public class CombatRoundEngine
    {
        private readonly IActionValidator _actionValidator;
        private readonly IActionExecutor _actionExecutor;
        private readonly ITurnManager _turnManager;
        private readonly EnemyIntentResolver _intentResolver;
        private readonly RoundLifecycleProcessor _roundLifecycle;
        private readonly BattlefieldFactory _battlefieldFactory;
        private readonly HexDirectionConfig _hexConfig;
        private readonly IReadOnlyList<IWinCondition> _winConditions;
        private readonly ICombatRoundFlow _flow;
        private readonly IGameLogger _logger;

        private ICombatState _gameState;
        private IBattlefield _battlefield;
        private int _resolvedIntentCount;
        private CombatInitiator _openingInitiator = CombatInitiator.Enemy;

        public CombatRoundEngine(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            EnemyIntentResolver intentResolver,
            RoundLifecycleProcessor roundLifecycle,
            BattlefieldFactory battlefieldFactory,
            HexDirectionConfig hexConfig,
            IReadOnlyList<IWinCondition> winConditions,
            ICombatRoundFlow flow,
            IGameLogger logger)
        {
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnManager = turnManager;
            _intentResolver = intentResolver;
            _roundLifecycle = roundLifecycle;
            _battlefieldFactory = battlefieldFactory;
            _hexConfig = hexConfig;
            _winConditions = winConditions;
            _flow = flow;
            _logger = logger;
        }

        // --- Read surface the controllers re-expose and the flows read ---

        public ICombatState State => _gameState;
        public ITurnManager TurnManager => _turnManager;
        public IBattlefield Battlefield => _battlefield;
        public CombatInitiator OpeningInitiator => _openingInitiator;
        public IActionValidator Validator => _actionValidator;
        public IActionExecutor Executor => _actionExecutor;
        public IGameLogger Logger => _logger;

        public event System.Action<ICombatState> OnStateChanged;
        public event System.Action<IPlayer> OnTurnStarted;
        public event System.Action<RoundPhase> OnRoundPhaseChanged;
        public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed;
        public event System.Action<IPlayer, CombatPhase> OnGameEnded;

        // --- Shared ICombatController mechanics (identical across PvE + Arena) ---

        public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players,
            CombatInitiator openingInitiator)
        {
            _openingInitiator = openingInitiator;
            _gameState = initialState;

            // Inject battlefield if it already exists.
            if (_battlefield != null && _gameState is CombatState withField)
            {
                _gameState = withField.WithBattlefield(_battlefield);
            }

            _turnManager.Initialize(players);

            _gameState = (_gameState as CombatState).WithPhase(CombatPhase.Combat);
            _gameState = (_gameState as CombatState).WithCurrentPlayer(_turnManager.CurrentPlayer);

            // Mode-specific bootstrap (Arena: host activate + transport subscribe; PvE: none).
            _flow.OnInitialized(this);

            // The round loop starts via BeginRounds() once all units are added — the first
            // Plan phase must see the full board.
            RaiseStateChanged();
        }

        public void BeginRounds()
        {
            _logger.Info(LogCategory.Combat, "[CombatRoundEngine] BeginRounds - starting the first round");
            StartRound();
        }

        public void AddUnit(IUnit unit)
        {
            if (unit == null)
            {
                _logger.Warning(LogCategory.Combat, "[CombatRoundEngine] Cannot add null unit to combat state");
                return;
            }

            // Validate that it's actually a Unit instance (prevent architectural violations).
            if (!(unit is Unit))
            {
                _logger.Error(LogCategory.Combat, $"[CombatRoundEngine] CombatState can only contain Unit instances. " +
                              $"Received: {unit.GetType().Name}. " +
                              $"MonoBehaviour adapters should add their internal Unit, not themselves.");
                return;
            }

            // Skip a duplicate id.
            if (_gameState.GetUnit(unit.Id) != null)
            {
                _logger.Warning(LogCategory.Combat, $"[CombatRoundEngine] Unit with ID {unit.Id} already exists in combat state. Skipping add.");
                return;
            }

            var newUnits = new List<IUnit>(_gameState.Units) { unit };
            _gameState = (_gameState as CombatState).WithUnits(newUnits);

            _logger.Info(LogCategory.Combat, $"[CombatRoundEngine] Added unit {unit.Id} (Owner: {unit.Owner?.Name ?? "null"}, Position: {unit.Position}) to combat state");
            RaiseStateChanged();
        }

        public ActionResult ProcessAction(IAction action)
        {
            return _flow.ProcessAction(this, action);
        }

        public void Update()
        {
            // Called each frame to settle win conditions (e.g. a status-tick death outside a round step).
            CheckWinConditions();
        }

        public bool ResolveNextEnemyIntent()
        {
            if (_gameState.Phase != CombatPhase.Combat)
                return false;
            if (_gameState.RoundPhase != RoundPhase.EnemyResolve)
                return false;

            if (_resolvedIntentCount >= _gameState.EnemyIntents.Count)
            {
                _flow.OnEnemyResolveFinished(this);
                return false;
            }

            var intent = _gameState.EnemyIntents[_resolvedIntentCount];
            _resolvedIntentCount++;

            _logger.Info(LogCategory.Combat, $"[CombatRoundEngine] Resolving enemy intent {_resolvedIntentCount}/{_gameState.EnemyIntents.Count} (unit {intent.UnitId}, {intent.Action.Type})");
            _gameState = _intentResolver.Resolve(_gameState, intent);

            RaiseStateChanged();
            _flow.OnIntentResolved(this, intent);
            CheckWinConditions();

            if (_gameState.Phase != CombatPhase.Combat)
                return false;

            if (_resolvedIntentCount >= _gameState.EnemyIntents.Count)
            {
                _flow.OnEnemyResolveFinished(this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Round bookkeeping (effects, cooldowns, acted flags) ticks once per round for ALL units — the ONE
        /// deterministic round-end status resolve point shared by PvE and Arena (combat-status-effects.md R3).
        /// The flow's <see cref="ICombatRoundFlow.OnRoundEndTail"/> runs after the tick and before the next
        /// round opens (Arena publishes the lockstep hash there).
        /// </summary>
        public void EndRound()
        {
            _gameState = _roundLifecycle.ApplyRoundEndEffects(_gameState);

            // The round-end status tick can kill (DoT): settle the outcome now instead of letting the next
            // frame's Update() catch it after a new round already started.
            RaiseStateChanged();
            CheckWinConditions();
            if (_gameState.Phase != CombatPhase.Combat)
                return;

            _turnManager.NextTurn();
            _gameState = (_gameState as CombatState).WithNextTurn();

            _gameState = _roundLifecycle.ResetUnitsForNewRound(_gameState);
            _gameState = _roundLifecycle.ApplyRoundStartEffects(_gameState);

            _flow.OnRoundEndTail(this);

            StartRound();
        }

        public void InitializeBattlefield(PlatformHexSurface surface, Vector3 center)
        {
            _battlefield = _battlefieldFactory.Create();
            _battlefield.Initialize(surface, center, _hexConfig);
            _battlefield.Activate();

            // Inject battlefield into existing state if state already exists.
            if (_gameState != null && _gameState is CombatState combatState)
            {
                _gameState = combatState.WithBattlefield(_battlefield);
                RaiseStateChanged();
            }
        }

        public void CleanupBattlefield()
        {
            // Mode-specific teardown before the battlefield drops (Arena: transport unsubscribe + host stop).
            _flow.OnCleanup(this);

            _battlefield?.Clear();
            _battlefield = null;
        }

        // --- Primitives the flows drive (shared building blocks, single-sourced here) ---

        /// <summary>Replaces the current state without raising <see cref="OnStateChanged"/>.</summary>
        public void SetState(ICombatState state)
        {
            _gameState = state;
        }

        /// <summary>Fires <see cref="OnStateChanged"/> with the current state.</summary>
        public void RaiseStateChanged()
        {
            OnStateChanged?.Invoke(_gameState);
        }

        /// <summary>Fires <see cref="OnEnemyPlansRevealed"/> with the round's committed intents.</summary>
        public void RaiseEnemyPlansRevealed(IReadOnlyList<EnemyIntent> intents)
        {
            OnEnemyPlansRevealed?.Invoke(intents);
        }

        public void SetRoundPhase(RoundPhase phase)
        {
            _gameState = (_gameState as CombatState).WithRoundPhase(phase);
            _logger.Info(LogCategory.Combat, $"[CombatRoundEngine] Round phase → {phase}");
            OnRoundPhaseChanged?.Invoke(phase);
        }

        /// <summary>
        /// Opens the player's Act phase: one <see cref="OnTurnStarted"/> per round keeps the existing UI
        /// consumers working. Shared by the PvE round-start / post-Resolve paths and the Arena planning open.
        /// </summary>
        public void BeginPlayerAct()
        {
            SetRoundPhase(RoundPhase.PlayerAct);
            OnTurnStarted?.Invoke(_turnManager.CurrentPlayer);
            RaiseStateChanged();
        }

        /// <summary>
        /// Enters the Resolve phase with a freshly supplied intent set and reveals it (the Arena path, where
        /// intents arrive on the round bundle rather than being planned at round start). Resets the resolve
        /// cursor: a fresh intent set always resolves from its first step — a no-op on the normal path (the
        /// cursor is already 0 after round start), load-bearing after an arena state rollback that abandons a
        /// half-resolved round (X1 migration / resync).
        /// </summary>
        public void EnterEnemyResolveWithIntents(IReadOnlyList<EnemyIntent> intents)
        {
            _resolvedIntentCount = 0;
            _gameState = (_gameState as CombatState).WithEnemyIntents(intents);
            RaiseEnemyPlansRevealed(intents);
            SetRoundPhase(RoundPhase.EnemyResolve);
            RaiseStateChanged();
        }

        public void CheckWinConditions()
        {
            // Once the game has ended, do not re-evaluate — this prevents a second OnGameEnded firing from a
            // later Update()/EndRound() call while the still-true win condition holds.
            if (_gameState.Phase != CombatPhase.Combat)
                return;

            foreach (var condition in _winConditions)
            {
                if (condition.Check(_gameState, out var winner))
                {
                    var newPhase = winner != null ? CombatPhase.Victory : CombatPhase.Defeat;
                    _gameState = (_gameState as CombatState).WithPhase(newPhase);

                    _logger.Info(LogCategory.Combat, winner != null
                        ? $"[CombatRoundEngine] Combat over — {winner.Name} wins"
                        : "[CombatRoundEngine] Combat over — no winner (defeat/draw)");

                    OnGameEnded?.Invoke(winner, newPhase);
                    RaiseStateChanged();
                    break;
                }
            }
        }

        /// <summary>
        /// Guarded round entry: reset the resolve cursor, then let the flow shape the round (PvE plans + leads;
        /// Arena opens the host gather + hidden planning).
        /// </summary>
        private void StartRound()
        {
            if (_gameState.Phase != CombatPhase.Combat)
                return;

            _resolvedIntentCount = 0;
            _flow.OnRoundStart(this);
        }
    }
}
