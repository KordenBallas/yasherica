using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Controller;
using Combat.Core;
using Combat.Data;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Enemy
{
    /// <summary>
    /// MonoBehaviour that integrates enemy GameObject with combat system.
    /// Enemy equivalent of CharacterCombatComponent.
    /// Implements IUnit interface by wrapping an internal Unit instance.
    /// Follows composition over inheritance pattern.
    /// Subscribes to CombatState changes to stay synchronized.
    /// </summary>
    public class EnemyCombatComponent : MonoBehaviour, IUnit
    {
        private Unit _internalUnit;
        private ICombatController _combatController;
        [Inject] private IGameLogger _logger;

        // IUnitIdentity
        public int Id => _internalUnit?.Id ?? -1;
        public IPlayer Owner => _internalUnit?.Owner;

        // IUnitPosition
        public HexCoordinates Position => _internalUnit?.Position ?? new HexCoordinates(0, 0);
        public HexCoordinates FacingDirection => _internalUnit?.FacingDirection ?? new HexCoordinates(1, 0);

        // IUnitHealth
        public int CurrentHP => _internalUnit?.CurrentHP ?? 0;
        public int MaxHP => _internalUnit?.MaxHP ?? 0;
        public bool IsAlive => _internalUnit?.IsAlive ?? false;

        // IUnitCombatant
        public IReadOnlyList<IAbilityInstance> Abilities => _internalUnit?.Abilities ?? new List<IAbilityInstance>();
        public IReadOnlyList<ScheduledAbility> AbilityQueue => _internalUnit?.AbilityQueue ?? new List<ScheduledAbility>();
        public IReadOnlyList<IStatusEffect> StatusEffects => _internalUnit?.StatusEffects ?? new List<IStatusEffect>();

        // IUnitActionState
        public bool HasActedThisTurn => _internalUnit?.HasActedThisTurn ?? false;
        public bool CanAct => _internalUnit?.CanAct ?? false;
        public UnitActionState ActionState => _internalUnit?.ActionState ?? UnitActionState.Dead;

        /// <summary>
        /// Indicates whether this component has been initialized for combat.
        /// Returns true if _internalUnit has been created via InitializeForCombat().
        /// </summary>
        public bool IsInitializedForCombat => _internalUnit != null;

        /// <summary>
        /// Initializes the enemy for combat.
        /// Creates internal Unit instance with enemy stats from EnemyData and subscribes to state changes.
        /// </summary>
        public void InitializeForCombat(
            int unitId,
            IPlayer owner,
            HexCoordinates startPosition,
            ICombatController combatController,
            EnemyData enemyData)
        {
            _combatController = combatController;

            // Create internal unit with enemy combat stats and abilities
            _internalUnit = new Unit(
                id: unitId,
                owner: owner,
                position: startPosition,
                currentHP: enemyData.MaxHP,
                maxHP: enemyData.MaxHP,
                abilities: enemyData.Abilities);

            // Subscribe to state changes for synchronization
            _combatController.OnStateChanged += OnCombatStateChanged;

            _logger?.Info(LogCategory.Combat,$"[EnemyCombatComponent] Initialized for combat: ID={unitId}, Name={enemyData.Name}, Position={startPosition}, HP={enemyData.MaxHP}");
            _logger?.Info(LogCategory.Combat,$"[EnemyCombatComponent] Abilities: {enemyData.Abilities.Count}, Owner={owner.Name}");
        }

        /// <summary>
        /// Synchronizes internal state when CombatState changes.
        /// Called automatically via OnStateChanged event subscription.
        /// </summary>
        private void OnCombatStateChanged(ICombatState newState)
        {
            if (_internalUnit == null)
            {
                return; // Not initialized yet
            }

            // Get updated unit from new state
            var updatedUnit = newState.GetUnit(_internalUnit.Id);

            if (updatedUnit == null)
            {
                _logger?.Warning(LogCategory.Combat,$"[EnemyCombatComponent] Unit {_internalUnit.Id} not found in updated state");
                return;
            }

            // Update internal reference (cast is safe - CombatState only stores Unit)
            var newUnit = updatedUnit as Unit;
            if (newUnit == null)
            {
                _logger?.Error(LogCategory.Combat,$"[EnemyCombatComponent] State contains non-Unit IUnit: {updatedUnit.GetType().Name}");
                return;
            }

            _internalUnit = newUnit;

            // Update GameObject visual position to match new hex position
            if (_combatController?.Battlefield != null)
            {
                Vector3 worldPosition = _combatController.Battlefield.HexToWorld(Position);
                transform.position = worldPosition;
                _logger?.Info(LogCategory.Combat,$"[EnemyCombatComponent] Unit {Id} synchronized: Position={Position}, WorldPos={worldPosition}, HP={CurrentHP}/{MaxHP}, Alive={IsAlive}");
            }
            else
            {
                _logger?.Info(LogCategory.Combat,$"[EnemyCombatComponent] Unit {Id} synchronized: Position={Position}, HP={CurrentHP}/{MaxHP}, Alive={IsAlive}");
            }
        }

        /// <summary>
        /// Provides access to the internal Unit for registration with CombatState.
        /// Used during initialization to add the pure C# Unit to CombatState.
        /// </summary>
        public IUnit InternalUnit => _internalUnit;

        // IUnitCombatant methods
        public IAbilityInstance GetAbility(int abilityId) => _internalUnit?.GetAbility(abilityId);
        public IReadOnlyList<IAbilityInstance> GetAvailableAbilities() => _internalUnit?.GetAvailableAbilities() ?? new List<IAbilityInstance>();
        public bool CanScheduleAbility() => _internalUnit?.CanScheduleAbility() ?? false;
        public bool CanMove() => _internalUnit?.CanMove() ?? false;

        /// <summary>
        /// Cleanup - unsubscribe from events to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (_combatController != null)
            {
                _combatController.OnStateChanged -= OnCombatStateChanged;
                _logger?.Info(LogCategory.Combat,$"[EnemyCombatComponent] Enemy {Id} destroyed and unsubscribed from OnStateChanged");
            }
        }
    }
}
