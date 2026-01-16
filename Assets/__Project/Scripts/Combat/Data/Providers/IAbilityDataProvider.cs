using System.Collections.Generic;
using Combat.Core;

namespace Combat.Data.Providers
{
    /// <summary>
    /// Provides ability data abstraction.
    /// Follows same pattern as IEnemyDataProvider.
    /// </summary>
    public interface IAbilityDataProvider
    {
        /// <summary>
        /// Gets an ability by its unique ID.
        /// </summary>
        /// <param name="abilityId">The ability's unique identifier.</param>
        /// <returns>The ability, or null if not found.</returns>
        IAbility GetAbility(int abilityId);

        /// <summary>
        /// Gets all available abilities.
        /// </summary>
        /// <returns>Read-only list of all abilities.</returns>
        IReadOnlyList<IAbility> GetAllAbilities();

        /// <summary>
        /// Gets abilities by effect type.
        /// </summary>
        /// <param name="effectType">The effect type to filter by.</param>
        /// <returns>Read-only list of matching abilities.</returns>
        IReadOnlyList<IAbility> GetAbilitiesByEffectType(AbilityEffectType effectType);
    }
}
