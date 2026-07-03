using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Character
{
    /// <summary>
    /// MonoBehaviour that integrates character GameObject with combat system.
    /// Implements IUnit interface by wrapping an internal Unit instance.
    /// Follows composition over inheritance pattern.
    /// Subscribes to CombatState changes to stay synchronized.
    /// </summary>
    public class CharacterCombatComponent : MonoBehaviour, IUnit
    {
        private Unit _internalUnit;
        private ICombatController _combatController;
        private float _feetOffset;
        [Inject] private IGameLogger _logger;

        // IUnitIdentity
        public int Id => _internalUnit?.Id ?? -1;
        public IPlayer Owner => _internalUnit?.Owner;
        
        // IUnitPosition
        public HexCoordinates Position => _internalUnit?.Position ?? new HexCoordinates(0, 0);
        public HexDirection FacingDirection => _internalUnit?.FacingDirection ?? HexDirection.E;
        
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
        /// Initializes the character for combat.
        /// Creates internal Unit instance with basic stats and subscribes to state changes.
        /// </summary>
        public void InitializeForCombat(
            int unitId,
            IPlayer owner,
            HexCoordinates startPosition,
            ICombatController combatController,
            int maxHP = 100)
        {
            var emptyAbilities = new List<IAbilityInstance>();
            InitializeForCombat(unitId, owner, startPosition, combatController, maxHP, emptyAbilities);
        }

        /// <summary>
        /// Initializes the character for combat with specific abilities.
        /// Creates internal Unit instance with provided abilities and subscribes to state changes.
        /// </summary>
        public void InitializeForCombat(
            int unitId,
            IPlayer owner,
            HexCoordinates startPosition,
            ICombatController combatController,
            int maxHP,
            IReadOnlyList<IAbilityInstance> abilities,
            IReadOnlyList<IStatusEffect> passiveEffects = null)
        {
            _combatController = combatController;
            _feetOffset = UnitGrounding.FeetOffsetFor(transform);

            // Create internal unit with combat stats, abilities, and any standing passive
            // modifiers granted by equipped parts (applied for the whole combat).
            _internalUnit = new Unit(
                id: unitId,
                owner: owner,
                position: startPosition,
                currentHP: maxHP,
                maxHP: maxHP,
                abilities: abilities,
                statusEffects: passiveEffects);

            // Subscribe to state changes for synchronization
            _combatController.OnStateChanged += OnCombatStateChanged;

            _logger?.Info(LogCategory.Character,$"[CharacterCombatComponent] Initialized for combat: ID={unitId}, Position={startPosition}, Owner={owner.Name}, Abilities={abilities.Count}, Passives={passiveEffects?.Count ?? 0}");
            _logger?.Info(LogCategory.Character,$"[CharacterCombatComponent] Subscribed to OnStateChanged");
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
                _logger?.Warning(LogCategory.Character,$"[CharacterCombatComponent] Unit {_internalUnit.Id} not found in updated state");
                return;
            }

            // Update internal reference (cast is safe - CombatState only stores Unit)
            var newUnit = updatedUnit as Unit;
            if (newUnit == null)
            {
                _logger?.Error(LogCategory.Character,$"[CharacterCombatComponent] State contains non-Unit IUnit: {updatedUnit.GetType().Name}");
                return;
            }

            _internalUnit = newUnit;

            // Update GameObject visual position to match new hex position
            if (_combatController?.Battlefield != null)
            {
                Vector3 worldPosition = UnitGrounding.Grounded(
                    _combatController.Battlefield.HexToWorld(Position), _feetOffset);
                transform.position = worldPosition;
                _logger?.Info(LogCategory.Character,$"[CharacterCombatComponent] Synchronized: Position={Position}, WorldPos={worldPosition}, HP={CurrentHP}/{MaxHP}");
            }
            else
            {
                _logger?.Info(LogCategory.Character,$"[CharacterCombatComponent] Synchronized: Position={Position}, HP={CurrentHP}/{MaxHP}");
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
                _logger?.Info(LogCategory.Character,"[CharacterCombatComponent] Unsubscribed from OnStateChanged");
            }
        }
    }
}
