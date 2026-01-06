using System.Collections.Generic;
using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Represents a combat unit on the battlefield.
    /// Tracks position, abilities, status effects, and turn state.
    /// </summary>
    public interface IUnit : IHasHealth
    {
        /// <summary>
        /// Unique identifier for this unit.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The player who owns this unit.
        /// </summary>
        IPlayer Owner { get; }
        
        /// <summary>
        /// Current position on the battlefield.
        /// </summary>
        HexCoordinates Position { get; }
        
        /// <summary>
        /// All abilities this unit possesses.
        /// </summary>
        IReadOnlyList<IAbilityInstance> Abilities { get; }
        
        /// <summary>
        /// Queue of scheduled abilities awaiting execution.
        /// </summary>
        IReadOnlyList<ScheduledAbility> AbilityQueue { get; }
        
        /// <summary>
        /// Active status effects on this unit.
        /// </summary>
        IReadOnlyList<IStatusEffect> StatusEffects { get; }
        
        /// <summary>
        /// Indicates whether this unit has acted this turn.
        /// </summary>
        bool HasActedThisTurn { get; }
        
        /// <summary>
        /// Indicates whether this unit can perform actions.
        /// </summary>
        bool CanAct { get; }
        
        /// <summary>
        /// Current action state of the unit.
        /// </summary>
        UnitActionState ActionState { get; }
        
        /// <summary>
        /// Indicates whether this unit can move.
        /// </summary>
        bool CanMove();
        
        /// <summary>
        /// Gets a specific ability by ID.
        /// </summary>
        IAbilityInstance GetAbility(int abilityId);
        
        /// <summary>
        /// Gets all abilities that are currently available (not on cooldown).
        /// </summary>
        IReadOnlyList<IAbilityInstance> GetAvailableAbilities();
        
        /// <summary>
        /// Indicates whether this unit can schedule another ability this turn.
        /// </summary>
        bool CanScheduleAbility();
    }
}

