using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Builds the 2-3 stage-up mutation options offered to the player from this stage's dominant
    /// archetypes. Pure C#, deterministic, and unit-testable (CLAUDE.md §10).
    /// </summary>
    public interface IMutationOptionBuilder
    {
        /// <summary>
        /// Gathers candidate options across <paramref name="dominantArchetypeIds"/> (already ordered
        /// strongest-first by the tally), in each archetype's authored option order. Skips any option
        /// whose part is already equipped (<paramref name="equippedPartIds"/>) or whose part was
        /// already chosen by a stronger archetype, and stops at <paramref name="maxOptions"/>.
        /// Returns 0..<paramref name="maxOptions"/> options.
        /// </summary>
        IReadOnlyList<MutationOption> Build(
            IReadOnlyList<string> dominantArchetypeIds,
            IMutationOptionProvider options,
            ISet<string> equippedPartIds,
            int maxOptions);
    }
}
