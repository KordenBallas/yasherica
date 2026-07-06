using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Core.Logging;
using Combat.Execution;
using Combat.TurnManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Combat.Controller
{
    /// <summary>
    /// Concrete implementation of combat controller.
    /// Orchestrates the Plan → Act → Resolve round: enemies commit and reveal intents at
    /// round start, the player acts freely against the live board, then committed enemy
    /// intents fire as shown. Also owns action processing, win checks, and the battlefield.
    /// </summary>
    public class CombatController : ICombatController
    {
        private readonly IActionValidator _actionValidator;
        private readonly IActionExecutor _actionExecutor;
        private readonly ITurnManager _turnManager;
        private readonly EnemyIntentPlanner _intentPlanner;
        private readonly EnemyIntentResolver _intentResolver;
        private readonly BattlefieldFactory _battlefieldFactory;
        private readonly HexDirectionConfig _hexConfig;
        private readonly List<IWinCondition> _winConditions;
        private readonly RoundLifecycleProcessor _roundLifecycle;
        private readonly IGameLogger _logger;

        private ICombatState _gameState;
        private IBattlefield _battlefield;
        private int _resolvedIntentCount;
        private CombatInitiator _openingInitiator = CombatInitiator.Enemy;

        // Per-round bookkeeping for the initiator-leads ordering (D2): a round runs its two acting
        // phases (player Act, enemy Resolve) in the order the round's lead dictates, then ends once
        // both have happened. These track which of the two has already occurred this round.
        private bool _playerActedThisRound;
        private bool _enemiesResolvedThisRound;

        public ICombatState CombatState => _gameState;
        public ITurnManager TurnManager => _turnManager;
        public IBattlefield Battlefield => _battlefield;
        public CombatInitiator OpeningInitiator => _openingInitiator;

        public event System.Action<ICombatState> OnStateChanged;
        public event System.Action<IPlayer> OnTurnStarted;
        public event System.Action<RoundPhase> OnRoundPhaseChanged;
        public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed;
        public event System.Action<IPlayer, CombatPhase> OnGameEnded;

        public CombatController(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            IDamageSystem damageSystem,
            StatusEffectTriggerProcessor triggerProcessor,
            EnemyIntentPlanner intentPlanner,
            EnemyIntentResolver intentResolver,
            BattlefieldFactory battlefieldFactory,
            HexDirectionConfig hexConfig,
            IGameLogger logger)
        {
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnManager = turnManager;
            _intentPlanner = intentPlanner;
            _intentResolver = intentResolver;
            _battlefieldFactory = battlefieldFactory;
            _hexConfig = hexConfig;
            _winConditions = new List<IWinCondition>();
            // Built internally (not injected) so the extraction stays signature-preserving for
            // every existing construction site; Arena binds its own instance in its installer.
            _roundLifecycle = new RoundLifecycleProcessor(damageSystem, triggerProcessor);
            _logger = logger;
        }

        public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players,
            CombatInitiator openingInitiator = CombatInitiator.Enemy)
        {
            _openingInitiator = openingInitiator;
            _gameState = initialState;

            // Inject battlefield if it already exists
            if (_battlefield != null && _gameState is CombatState combatState)
            {
                _gameState = combatState.WithBattlefield(_battlefield);
            }

            _turnManager.Initialize(players);

            // Start combat phase
            _gameState = (_gameState as CombatState).WithPhase(CombatPhase.Combat);
            _gameState = (_gameState as CombatState).WithCurrentPlayer(_turnManager.CurrentPlayer);

            // Register default win condition
            _winConditions.Add(new EliminateAllEnemiesWinCondition());

            // The round loop starts via BeginRounds() once all units are added — the first
            // Plan phase must see the full board.
            OnStateChanged?.Invoke(_gameState);
        }

        public void BeginRounds()
        {
            _logger.Info(LogCategory.Combat,"[CombatController] BeginRounds - starting the first round");
            StartRound();
        }
        
        public void AddUnit(IUnit unit)
        {
            if (unit == null)
            {
                _logger.Warning(LogCategory.Combat,"[CombatController] Cannot add null unit to combat state");
                return;
            }

            // Validate that it's actually a Unit instance (prevent architectural violations)
            if (!(unit is Unit))
            {
                _logger.Error(LogCategory.Combat,$"[CombatController] CombatState can only contain Unit instances. " +
                              $"Received: {unit.GetType().Name}. " +
                              $"MonoBehaviour adapters should add their internal Unit, not themselves.");
                return;
            }

            // Check if unit with same ID already exists
            var existingUnit = _gameState.GetUnit(unit.Id);
            if (existingUnit != null)
            {
                _logger.Warning(LogCategory.Combat,$"[CombatController] Unit with ID {unit.Id} already exists in combat state. Skipping add.");
                return;
            }
            
            // Create new units list with existing units plus the new unit
            var newUnits = new List<IUnit>(_gameState.Units) { unit };
            
            // Create new immutable state with updated units
            _gameState = (_gameState as CombatState).WithUnits(newUnits);
            
            _logger.Info(LogCategory.Combat,$"[CombatController] Added unit {unit.Id} (Owner: {unit.Owner?.Name ?? "null"}, Position: {unit.Position}) to combat state");
            
            // Trigger state change event
            OnStateChanged?.Invoke(_gameState);
        }
        
        public ActionResult ProcessAction(IAction action)
        {
            _logger.Info(LogCategory.Combat,$"[CombatController] ProcessAction called: Action={action.Type}, UnitId={action.UnitId}, ActionPlayerId={action.Player?.Id}");
            _logger.Info(LogCategory.Combat,$"[CombatController] Current turn player: {_turnManager.CurrentPlayer?.Id} ({_turnManager.CurrentPlayer?.Name})");

            var unit = _gameState.GetUnit(action.UnitId);
            if (unit != null)
            {
                _logger.Info(LogCategory.Combat,$"[CombatController] Unit {unit.Id} Owner: {unit.Owner?.Id} ({unit.Owner?.Name})");
            }

            // Validate action
            var validationResult = _actionValidator.ValidateDetailed(_gameState, action);
            if (!validationResult.IsValid)
            {
                return ActionResult.Failed(_gameState, validationResult.FailureReason);
            }

            // Execute action
            var result = _actionExecutor.ExecuteWithResult(_gameState, action);
            _gameState = result.NewState;

            OnStateChanged?.Invoke(_gameState);

            // Check for turn end
            CheckTurnEnd();

            // Check win conditions
            CheckWinConditions();

            return result;
        }
        
        public void Update()
        {
            // This can be called each frame to check conditions
            CheckWinConditions();
        }
        
        /// <summary>
        /// Plan phase: every enemy commits and reveals its intent, then the round's lead acts first —
        /// the player's Act phase for a player-led round, or the enemy Resolve phase for an enemy-led
        /// opening round (D2). The other acting phase follows; the round ends once both have happened.
        /// </summary>
        private void StartRound()
        {
            if (_gameState.Phase != CombatPhase.Combat)
                return;

            _resolvedIntentCount = 0;
            _playerActedThisRound = false;
            _enemiesResolvedThisRound = false;
            SetRoundPhase(RoundPhase.EnemyPlan);

            var intents = _intentPlanner.Plan(_gameState);
            _gameState = (_gameState as CombatState).WithEnemyIntents(intents);
            OnEnemyPlansRevealed?.Invoke(intents);

            if (RoundLeadPolicy.EnemyLeadsThisRound(_gameState.TurnNumber, _openingInitiator))
            {
                // Enemy-initiated opening round: committed intents resolve before the player acts.
                SetRoundPhase(RoundPhase.EnemyResolve);
                OnStateChanged?.Invoke(_gameState);
            }
            else
            {
                BeginPlayerAct();
            }
        }

        /// <summary>
        /// Opens the player's Act phase. One OnTurnStarted per round keeps the existing UI consumers
        /// working; shared by the round-start path (player-led) and the post-Resolve path (enemy-led).
        /// </summary>
        private void BeginPlayerAct()
        {
            SetRoundPhase(RoundPhase.PlayerAct);
            OnTurnStarted?.Invoke(_turnManager.CurrentPlayer);
            OnStateChanged?.Invoke(_gameState);
        }

        public bool ResolveNextEnemyIntent()
        {
            if (_gameState.Phase != CombatPhase.Combat)
                return false;
            if (_gameState.RoundPhase != RoundPhase.EnemyResolve)
                return false;

            if (_resolvedIntentCount >= _gameState.EnemyIntents.Count)
            {
                FinishEnemyResolvePhase();
                return false;
            }

            var intent = _gameState.EnemyIntents[_resolvedIntentCount];
            _resolvedIntentCount++;

            _logger.Info(LogCategory.Combat,$"[CombatController] Resolving enemy intent {_resolvedIntentCount}/{_gameState.EnemyIntents.Count} (unit {intent.UnitId}, {intent.Action.Type})");
            _gameState = _intentResolver.Resolve(_gameState, intent);

            OnStateChanged?.Invoke(_gameState);
            CheckWinConditions();

            if (_gameState.Phase != CombatPhase.Combat)
                return false;

            if (_resolvedIntentCount >= _gameState.EnemyIntents.Count)
            {
                FinishEnemyResolvePhase();
                return false;
            }

            return true;
        }

        /// <summary>
        /// The enemy Resolve phase has fired every committed intent. If the player has not yet acted
        /// this round (an enemy-led opening round), open the player's Act phase; otherwise the round is
        /// complete. This is the second half of the initiator-leads ordering (D2).
        /// </summary>
        private void FinishEnemyResolvePhase()
        {
            _enemiesResolvedThisRound = true;

            if (!_playerActedThisRound)
                BeginPlayerAct();
            else
                EndRound();
        }

        /// <summary>
        /// Round bookkeeping (effects, cooldowns, acted flags) now ticks once per round for
        /// ALL units — the same cadence each unit had under round-robin, in one place.
        /// </summary>
        private void EndRound()
        {
            _gameState = _roundLifecycle.ApplyRoundEndEffects(_gameState);

            _turnManager.NextTurn();
            _gameState = (_gameState as CombatState).WithNextTurn();

            _gameState = _roundLifecycle.ResetUnitsForNewRound(_gameState);
            _gameState = _roundLifecycle.ApplyRoundStartEffects(_gameState);

            StartRound();
        }

        private void SetRoundPhase(RoundPhase phase)
        {
            _gameState = (_gameState as CombatState).WithRoundPhase(phase);
            _logger.Info(LogCategory.Combat,$"[CombatController] Round phase → {phase}");
            OnRoundPhaseChanged?.Invoke(phase);
        }

        private void CheckTurnEnd()
        {
            if (_gameState.RoundPhase != RoundPhase.PlayerAct)
                return;

            var currentPlayer = _turnManager.CurrentPlayer;
            var activeUnits = _gameState.GetActiveUnitsByPlayer(currentPlayer);

            if (activeUnits.Count == 0)
            {
                _playerActedThisRound = true;

                if (!_enemiesResolvedThisRound)
                {
                    _logger.Info(LogCategory.Combat,"[CombatController] All player units have acted - entering Resolve phase");
                    SetRoundPhase(RoundPhase.EnemyResolve);
                    OnStateChanged?.Invoke(_gameState);
                }
                else
                {
                    // Enemy-led opening round: enemies already resolved, so the player closing their
                    // Act ends the round (D2).
                    _logger.Info(LogCategory.Combat,"[CombatController] Player acted after enemy-led resolve - ending round");
                    EndRound();
                }
            }
        }

        private void CheckWinConditions()
        {
            foreach (var condition in _winConditions)
            {
                if (condition.Check(_gameState, out var winner))
                {
                    // Game over
                    var newPhase = winner != null ? CombatPhase.Victory : CombatPhase.Defeat;
                    _gameState = (_gameState as CombatState).WithPhase(newPhase);
                    
                    OnGameEnded?.Invoke(winner, newPhase);
                    OnStateChanged?.Invoke(_gameState);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Initializes the battlefield from the platform's hex surface.
        /// Called by CombatActiveState when platform is entered.
        /// Hex size and orientation ride on the surface itself (one source of truth).
        /// </summary>
        public void InitializeBattlefield(PlatformHexSurface surface, Vector3 center)
        {
            _battlefield = _battlefieldFactory.Create();
            _battlefield.Initialize(surface, center, _hexConfig);
            _battlefield.Activate();

            // Inject battlefield into existing state if state already exists
            if (_gameState != null && _gameState is CombatState combatState)
            {
                _gameState = combatState.WithBattlefield(_battlefield);
                OnStateChanged?.Invoke(_gameState);
            }
        }
        
        /// <summary>
        /// Cleans up battlefield when combat ends or platform is exited.
        /// Called by CombatActiveState.OnExit().
        /// </summary>
        public void CleanupBattlefield()
        {
            _battlefield?.Clear();
            _battlefield = null;
        }
    }
}

