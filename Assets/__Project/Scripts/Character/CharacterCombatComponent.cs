using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Core;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// MonoBehaviour that integrates character GameObject with combat system.
    /// Implements IUnit interface by wrapping an internal Unit instance.
    /// Follows composition over inheritance pattern.
    /// </summary>
    public class CharacterCombatComponent : MonoBehaviour, IUnit
    {
        private Unit _internalUnit;
        
        // IUnitIdentity
        public int Id => _internalUnit?.Id ?? -1;
        public IPlayer Owner => _internalUnit?.Owner;
        
        // IUnitPosition
        public HexCoordinates Position => _internalUnit?.Position ?? new HexCoordinates(0, 0);
        
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
        /// Creates internal Unit instance with basic stats.
        /// </summary>
        public void InitializeForCombat(
            int unitId,
            IPlayer owner, 
            HexCoordinates startPosition,
            int maxHP = 100)
        {
            // Create internal unit with basic combat stats
            var emptyAbilities = new List<IAbilityInstance>();
            _internalUnit = new Unit(
                id: unitId,
                owner: owner,
                position: startPosition,
                currentHP: maxHP,
                maxHP: maxHP,
                abilities: emptyAbilities);
            
            Debug.Log($"[CharacterCombatComponent] Initialized for combat: ID={unitId}, Position={startPosition}, Owner={owner.Name}");
        }
        
        /// <summary>
        /// Updates internal state from authoritative combat state.
        /// Called when CombatState changes.
        /// </summary>
        public void UpdateFromCombatState(IUnit updatedUnit)
        {
            if (_internalUnit == null)
            {
                Debug.LogError($"[CharacterCombatComponent] Cannot update: not initialized");
                return;
            }
            
            if (updatedUnit.Id != _internalUnit.Id)
            {
                Debug.LogError($"[CharacterCombatComponent] Trying to update with wrong unit ID: expected {_internalUnit.Id}, got {updatedUnit.Id}");
                return;
            }
            
            _internalUnit = updatedUnit as Unit;
            Debug.Log($"[CharacterCombatComponent] Updated from combat state: Position={Position}, HP={CurrentHP}/{MaxHP}");
        }
        
        // IUnitCombatant methods
        public IAbilityInstance GetAbility(int abilityId) => _internalUnit?.GetAbility(abilityId);
        public IReadOnlyList<IAbilityInstance> GetAvailableAbilities() => _internalUnit?.GetAvailableAbilities() ?? new List<IAbilityInstance>();
        public bool CanScheduleAbility() => _internalUnit?.CanScheduleAbility() ?? false;
        public bool CanMove() => _internalUnit?.CanMove() ?? false;
    }
}
