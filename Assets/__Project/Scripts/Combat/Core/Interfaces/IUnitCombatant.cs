using System.Collections.Generic;

namespace Combat.Core
{
    /// <summary>
    /// Represents unit combat capabilities (abilities, effects).
    /// Part of interface segregation for IUnit.
    /// </summary>
    public interface IUnitCombatant
    {
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
