using Combat.Core;
using Combat.Data.Definitions;

namespace Combat.Data.Factories
{
    /// <summary>
    /// Factory interface for creating runtime status effect instances from ScriptableObject definitions.
    /// </summary>
    public interface IStatusEffectFactory
    {
        /// <summary>
        /// Creates a runtime IStatusEffect from a ScriptableObject definition.
        /// </summary>
        /// <param name="definition">The status effect definition.</param>
        /// <returns>A runtime status effect instance.</returns>
        IStatusEffect CreateStatusEffect(StatusEffectDefinition definition);

        /// <summary>
        /// Creates a runtime IStatusEffect with a custom duration override.
        /// </summary>
        /// <param name="definition">The status effect definition.</param>
        /// <param name="durationOverride">Custom duration to use instead of the definition's default.</param>
        /// <returns>A runtime status effect instance.</returns>
        IStatusEffect CreateStatusEffect(StatusEffectDefinition definition, int durationOverride);

        /// <summary>
        /// Creates a runtime IStatusEffect at an explicit duration AND stack count — the arena
        /// state-transfer path, where a mid-life effect is rebuilt from its snapshot triple.
        /// </summary>
        IStatusEffect CreateStatusEffect(StatusEffectDefinition definition, int durationOverride, int stackCount);
    }
}
