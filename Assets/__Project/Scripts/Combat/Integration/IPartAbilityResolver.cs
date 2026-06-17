using System.Collections.Generic;

namespace Combat.Integration
{
    /// <summary>
    /// Resolves the combat ability set granted by a set of equipped body parts.
    /// The bridge from the modular character's equipped parts into combat: parts are the
    /// source of truth for what the player can do in combat (mutating the body changes it).
    /// </summary>
    public interface IPartAbilityResolver
    {
        /// <summary>
        /// Collects the active + passive abilities declared by the given equipped part ids,
        /// deduplicated. Unknown part ids are skipped (and logged).
        /// </summary>
        PartAbilitySet Resolve(IEnumerable<string> equippedPartIds);
    }
}
