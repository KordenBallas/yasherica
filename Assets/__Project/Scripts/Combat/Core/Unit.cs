using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Concrete implementation of a combat unit.
    /// Immutable - all modifications return new instances.
    /// </summary>
    public class Unit : IUnit
    {
        public int Id { get; }
        public IPlayer Owner { get; }
        public HexCoordinates Position { get; }
        public HexCoordinates FacingDirection { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public bool IsAlive => CurrentHP > 0;
        public IReadOnlyList<IAbilityInstance> Abilities { get; }
        public IReadOnlyList<ScheduledAbility> AbilityQueue { get; }
        public IReadOnlyList<IStatusEffect> StatusEffects { get; }
        public bool HasActedThisTurn { get; }
        
        public bool CanAct => ActionState == UnitActionState.Ready;
        
        public UnitActionState ActionState
        {
            get
            {
                if (!IsAlive)
                    return UnitActionState.Dead;
                    
                if (StatusEffects.Any(e => e is StunEffect))
                    return UnitActionState.Stunned;
                    
                if (HasActedThisTurn)
                    return UnitActionState.ActedThisTurn;
                    
                return UnitActionState.Ready;
            }
        }
        
        public Unit(
            int id,
            IPlayer owner,
            HexCoordinates position,
            int currentHP,
            int maxHP,
            IReadOnlyList<IAbilityInstance> abilities,
            IReadOnlyList<ScheduledAbility> abilityQueue = null,
            IReadOnlyList<IStatusEffect> statusEffects = null,
            bool hasActedThisTurn = false,
            HexCoordinates? facingDirection = null)
        {
            Id = id;
            Owner = owner;
            Position = position;
            FacingDirection = facingDirection ?? new HexCoordinates(1, 0); // Default: East
            CurrentHP = currentHP;
            MaxHP = maxHP;
            Abilities = abilities ?? new List<IAbilityInstance>();
            AbilityQueue = abilityQueue ?? new List<ScheduledAbility>();
            StatusEffects = statusEffects ?? new List<IStatusEffect>();
            HasActedThisTurn = hasActedThisTurn;
        }
        
        public bool CanMove()
        {
            return CanAct && !HasActedThisTurn;
        }
        
        public IAbilityInstance GetAbility(int abilityId)
        {
            return Abilities.FirstOrDefault(a => a.Ability.Id == abilityId);
        }
        
        public IReadOnlyList<IAbilityInstance> GetAvailableAbilities()
        {
            return Abilities.Where(a => a.IsAvailable).ToList();
        }
        
        public bool CanScheduleAbility()
        {
            // Maximum 1 new ability can be scheduled per turn
            // This is tracked externally by the validator based on turn state
            return CanAct && !HasActedThisTurn;
        }
        
        /// <summary>
        /// Creates a new unit with updated position.
        /// </summary>
        public Unit WithPosition(HexCoordinates newPosition)
        {
            return new Unit(Id, Owner, newPosition, CurrentHP, MaxHP, Abilities, AbilityQueue, StatusEffects, HasActedThisTurn, FacingDirection);
        }

        /// <summary>
        /// Creates a new unit with updated facing direction.
        /// </summary>
        public Unit WithFacingDirection(HexCoordinates newFacingDirection)
        {
            return new Unit(Id, Owner, Position, CurrentHP, MaxHP, Abilities, AbilityQueue, StatusEffects, HasActedThisTurn, newFacingDirection);
        }

        /// <summary>
        /// Creates a new unit with updated HP.
        /// </summary>
        public Unit WithHP(int newHP)
        {
            return new Unit(Id, Owner, Position, System.Math.Max(0, System.Math.Min(newHP, MaxHP)), MaxHP, Abilities, AbilityQueue, StatusEffects, HasActedThisTurn, FacingDirection);
        }

        /// <summary>
        /// Creates a new unit with HasActedThisTurn set.
        /// </summary>
        public Unit WithActedThisTurn(bool acted)
        {
            return new Unit(Id, Owner, Position, CurrentHP, MaxHP, Abilities, AbilityQueue, StatusEffects, acted, FacingDirection);
        }

        /// <summary>
        /// Creates a new unit with updated abilities (e.g., after cooldown changes).
        /// </summary>
        public Unit WithAbilities(IReadOnlyList<IAbilityInstance> newAbilities)
        {
            return new Unit(Id, Owner, Position, CurrentHP, MaxHP, newAbilities, AbilityQueue, StatusEffects, HasActedThisTurn, FacingDirection);
        }

        /// <summary>
        /// Creates a new unit with updated ability queue.
        /// </summary>
        public Unit WithAbilityQueue(IReadOnlyList<ScheduledAbility> newQueue)
        {
            return new Unit(Id, Owner, Position, CurrentHP, MaxHP, Abilities, newQueue, StatusEffects, HasActedThisTurn, FacingDirection);
        }

        /// <summary>
        /// Creates a new unit with updated status effects.
        /// </summary>
        public Unit WithStatusEffects(IReadOnlyList<IStatusEffect> newEffects)
        {
            return new Unit(Id, Owner, Position, CurrentHP, MaxHP, Abilities, AbilityQueue, newEffects, HasActedThisTurn, FacingDirection);
        }
    }
}

