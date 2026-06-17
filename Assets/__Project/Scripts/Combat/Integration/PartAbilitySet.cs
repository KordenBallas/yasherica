using System;
using System.Collections.Generic;
using Combat.Data.Definitions;

namespace Combat.Integration
{
    /// <summary>
    /// The combat abilities granted by a character's currently equipped parts:
    /// active abilities (usable in combat) and passive abilities (standing modifiers).
    /// Deduplicated; order follows the order parts were enumerated.
    /// </summary>
    public sealed class PartAbilitySet
    {
        public IReadOnlyList<AbilityDefinition> ActiveAbilities { get; }
        public IReadOnlyList<PassiveAbilityDefinition> PassiveAbilities { get; }

        public bool IsEmpty => ActiveAbilities.Count == 0 && PassiveAbilities.Count == 0;

        public PartAbilitySet(
            IReadOnlyList<AbilityDefinition> activeAbilities,
            IReadOnlyList<PassiveAbilityDefinition> passiveAbilities)
        {
            ActiveAbilities = activeAbilities ?? Array.Empty<AbilityDefinition>();
            PassiveAbilities = passiveAbilities ?? Array.Empty<PassiveAbilityDefinition>();
        }

        public static readonly PartAbilitySet Empty = new PartAbilitySet(
            Array.Empty<AbilityDefinition>(),
            Array.Empty<PassiveAbilityDefinition>());
    }
}
