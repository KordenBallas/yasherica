using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Builds the stage-up mutation options offered to the player by scoring every candidate part
    /// against this stage's cumulative feed tally and taking the best ones. Pure C#, deterministic,
    /// and unit-testable (CLAUDE.md §10).
    /// </summary>
    public interface IMutationOptionBuilder
    {
        /// <summary>
        /// Scores each part in <paramref name="candidates"/> against <paramref name="tallyTotals"/>
        /// (archetype id -> accumulated weight this stage) plus a rarity bonus governed by
        /// <paramref name="scoring"/>, skips parts already equipped (<paramref name="equippedPartIds"/>)
        /// and parts scoring at or below zero, then returns the top <paramref name="maxOptions"/> in
        /// descending score with a deterministic ordinal part-id tie-break. Returns
        /// 0..<paramref name="maxOptions"/> options.
        /// </summary>
        IReadOnlyList<MutationOption> Build(
            IReadOnlyDictionary<string, float> tallyTotals,
            IReadOnlyList<MutationCandidatePart> candidates,
            ISet<string> equippedPartIds,
            int maxOptions,
            MutationScoringParameters scoring);
    }
}
