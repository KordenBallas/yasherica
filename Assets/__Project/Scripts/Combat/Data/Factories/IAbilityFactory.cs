using Combat.Core;
using Combat.Data.Definitions;

namespace Combat.Data.Factories
{
    /// <summary>
    /// Factory interface for creating runtime ability instances from ScriptableObject definitions.
    /// </summary>
    public interface IAbilityFactory
    {
        /// <summary>
        /// Creates a runtime IAbility from a ScriptableObject definition.
        /// </summary>
        /// <param name="definition">The ability definition.</param>
        /// <returns>A runtime ability instance.</returns>
        IAbility CreateAbility(AbilityDefinition definition);

        /// <summary>
        /// Creates an AbilityInstance wrapper with cooldown tracking.
        /// </summary>
        /// <param name="definition">The ability definition.</param>
        /// <returns>An ability instance ready for use by a unit.</returns>
        IAbilityInstance CreateAbilityInstance(AbilityDefinition definition);
    }
}
