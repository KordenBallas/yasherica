using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.TurnManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Combat.Controller
{
    /// <summary>
    /// Concrete implementation of combat controller.
    /// Orchestrates turn cycle, action processing, win condition checking, and battlefield management.
    /// </summary>
    public class CombatController : ICombatController
    {
        private readonly IActionValidator _actionValidator;
        private readonly IActionExecutor _actionExecutor;
        private readonly ITurnManager _turnManager;
        private readonly IDamageSystem _damageSystem;
        private readonly BattlefieldFactory _battlefieldFactory;
        private readonly CombatConfig _config;
        private readonly HexDirectionConfig _hexConfig;
        private readonly List<IWinCondition> _winConditions;

        private ICombatState _gameState;
        private IBattlefield _battlefield;

        public ICombatState CombatState => _gameState;
        public ITurnManager TurnManager => _turnManager;
        public IBattlefield Battlefield => _battlefield;

        public event System.Action<ICombatState> OnStateChanged;
        public event System.Action<IPlayer> OnTurnStarted;
        public event System.Action<IPlayer, CombatPhase> OnGameEnded;

        public CombatController(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            IDamageSystem damageSystem,
            BattlefieldFactory battlefieldFactory,
            CombatConfig config,
            HexDirectionConfig hexConfig)
        {
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnManager = turnManager;
            _damageSystem = damageSystem;
            _battlefieldFactory = battlefieldFactory;
            _config = config;
            _hexConfig = hexConfig;
            _winConditions = new List<IWinCondition>();
        }
        
        public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players)
        {
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
            //_winConditions.Add(new EliminateAllEnemiesWinCondition());
            _winConditions.Add(new SurviveTurnsWinCondition(10, players.First().Id));
            
            // Trigger turn start
            OnTurnStarted?.Invoke(_turnManager.CurrentPlayer);
            OnStateChanged?.Invoke(_gameState);
        }
        
        public void AddUnit(IUnit unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("[CombatController] Cannot add null unit to combat state");
                return;
            }

            // Validate that it's actually a Unit instance (prevent architectural violations)
            if (!(unit is Unit))
            {
                Debug.LogError($"[CombatController] CombatState can only contain Unit instances. " +
                              $"Received: {unit.GetType().Name}. " +
                              $"MonoBehaviour adapters should add their internal Unit, not themselves.");
                return;
            }

            // Check if unit with same ID already exists
            var existingUnit = _gameState.GetUnit(unit.Id);
            if (existingUnit != null)
            {
                Debug.LogWarning($"[CombatController] Unit with ID {unit.Id} already exists in combat state. Skipping add.");
                return;
            }
            
            // Create new units list with existing units plus the new unit
            var newUnits = new List<IUnit>(_gameState.Units) { unit };
            
            // Create new immutable state with updated units
            _gameState = (_gameState as CombatState).WithUnits(newUnits);
            
            Debug.Log($"[CombatController] Added unit {unit.Id} (Owner: {unit.Owner?.Name ?? "null"}, Position: {unit.Position}) to combat state");
            
            // Trigger state change event
            OnStateChanged?.Invoke(_gameState);
        }
        
        public ActionResult ProcessAction(IAction action)
        {
            Debug.Log($"[CombatController] ProcessAction called: Action={action.Type}, UnitId={action.UnitId}, ActionPlayerId={action.Player?.Id}");
            Debug.Log($"[CombatController] Current turn player: {_turnManager.CurrentPlayer?.Id} ({_turnManager.CurrentPlayer?.Name})");

            var unit = _gameState.GetUnit(action.UnitId);
            if (unit != null)
            {
                Debug.Log($"[CombatController] Unit {unit.Id} Owner: {unit.Owner?.Id} ({unit.Owner?.Name})");
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
        
        private void CheckTurnEnd()
        {
            var currentPlayer = _turnManager.CurrentPlayer;

            Debug.Log($"[CombatController] CheckTurnEnd called - Current Player: {currentPlayer?.Id} ({currentPlayer?.Name})");

            // Get all units belonging to current player
            var playerUnits = _gameState.GetUnitsByPlayer(currentPlayer);
            Debug.Log($"[CombatController] Total units for player {currentPlayer?.Id}: {playerUnits.Count}");

            foreach (var unit in playerUnits)
            {
                Debug.Log($"[CombatController] Unit {unit.Id}: IsAlive={unit.IsAlive}, HasActedThisTurn={unit.HasActedThisTurn}, CanAct={unit.CanAct}, ActionState={unit.ActionState}");
            }

            // Check if all units of current player have acted
            var activeUnits = _gameState.GetActiveUnitsByPlayer(currentPlayer);

            Debug.Log($"[CombatController] Active units remaining for player {currentPlayer?.Id}: {activeUnits.Count}");

            if (activeUnits.Count == 0)
            {
                Debug.Log($"[CombatController] All units have acted - advancing turn");
                // All units have acted, advance turn
                AdvanceTurn();
            }
            else
            {
                Debug.Log($"[CombatController] Turn continues - {activeUnits.Count} unit(s) can still act");
                foreach (var unit in activeUnits)
                {
                    Debug.Log($"[CombatController] Active unit: {unit.Id}");
                }
            }
        }
        
        private void AdvanceTurn()
        {
            // Apply end-of-turn effects
            _gameState = ApplyTurnEndEffects(_gameState);
            
            // Switch to next player
            _turnManager.NextTurn();
            _gameState = (_gameState as CombatState).WithCurrentPlayer(_turnManager.CurrentPlayer);
            _gameState = (_gameState as CombatState).WithNextTurn();
            
            // Reset units for new turn
            _gameState = ResetUnitsForNewTurn(_gameState);
            
            // Apply start-of-turn effects
            _gameState = ApplyTurnStartEffects(_gameState);
            
            OnTurnStarted?.Invoke(_turnManager.CurrentPlayer);
            OnStateChanged?.Invoke(_gameState);
        }
        
        private ICombatState ResetUnitsForNewTurn(ICombatState gameState)
        {
            var currentPlayerUnits = gameState.GetUnitsByPlayer(_turnManager.CurrentPlayer);
            var newState = gameState;
            
            foreach (var unit in currentPlayerUnits)
            {
                // Reset HasActedThisTurn
                var updatedUnit = (unit as Unit).WithActedThisTurn(false);
                
                // Decrement ability cooldowns
                var newAbilities = updatedUnit.Abilities
                    .Select(a => (a as AbilityInstance).DecrementCooldown())
                    .ToList();
                updatedUnit = updatedUnit.WithAbilities(newAbilities);
                
                newState = (newState as CombatState).WithUpdatedUnit(updatedUnit);
            }
            
            return newState;
        }
        
        private ICombatState ApplyTurnStartEffects(ICombatState gameState)
        {
            var currentPlayerUnits = gameState.GetUnitsByPlayer(_turnManager.CurrentPlayer);
            var newState = gameState;
            
            foreach (var unit in currentPlayerUnits)
            {
                // Apply DOT/HOT effects
                newState = ApplyStatusEffects(newState, unit);
            }
            
            return newState;
        }
        
        private ICombatState ApplyTurnEndEffects(ICombatState gameState)
        {
            var currentPlayerUnits = gameState.GetUnitsByPlayer(_turnManager.CurrentPlayer);
            var newState = gameState;
            
            foreach (var unit in currentPlayerUnits)
            {
                // Decrement status effect durations
                newState = DecrementStatusEffects(newState, unit);
            }
            
            return newState;
        }
        
        private ICombatState ApplyStatusEffects(ICombatState gameState, IUnit unit)
        {
            var newState = gameState;
            
            foreach (var effect in unit.StatusEffects)
            {
                if (effect is PoisonEffect poison)
                {
                    newState = _damageSystem.ApplyDamage(newState, unit, poison.DamagePerTurn);
                }
                else if (effect is RegenerationEffect regen)
                {
                    newState = _damageSystem.ApplyHealing(newState, unit, regen.HealPerTurn);
                }
            }
            
            return newState;
        }
        
        private ICombatState DecrementStatusEffects(ICombatState gameState, IUnit unit)
        {
            var updatedUnit = gameState.GetUnit(unit.Id) as Unit;
            
            // Decrement durations and remove expired effects
            var newEffects = updatedUnit.StatusEffects
                .Select(e => (e as StatusEffect).DecrementDuration())
                .Where(e => e.Duration > 0)
                .ToList<IStatusEffect>();
            
            updatedUnit = updatedUnit.WithStatusEffects(newEffects);
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
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
        /// Initializes the battlefield with geometric data from platform.
        /// Called by CombatPlatformActiveState when platform is entered.
        /// Uses Combat configuration for hex size and orientation.
        /// </summary>
        public void InitializeBattlefield(List<Vector3> boundary, Vector3 center)
        {
            _battlefield = _battlefieldFactory.Create();
            _battlefield.Initialize(
                boundary,
                center,
                _config.HexCellSize,
                _config.HexOrientation,
                _hexConfig);
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
        /// Called by CombatPlatformActiveState.OnExit().
        /// </summary>
        public void CleanupBattlefield()
        {
            _battlefield?.Clear();
            _battlefield = null;
        }
    }
}

