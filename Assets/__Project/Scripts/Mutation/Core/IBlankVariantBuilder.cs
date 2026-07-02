using System.Collections.Generic;
using Inventory.Core;

namespace Mutation.Core
{
    /// <summary>
    /// Builds the unseal variant menu of a ready blank: authored parts for the
    /// blank's slot, deterministically scored against the socketed reagents'
    /// combined trait profile. Direction is readable from what was socketed; the
    /// exact menu is only revealed here, at unseal.
    /// </summary>
    public interface IBlankVariantBuilder
    {
        /// <summary>
        /// Top-scored variant options for the blank (at most maxOptions). There is
        /// deliberately no zero-score filter: raw-only socketing yields weak but
        /// never empty menus while any non-equipped part exists for the slot.
        /// </summary>
        IReadOnlyList<MutationOption> Build(
            PartBlankData blank,
            IReadOnlyList<ArtifactTraitProfile> socketedProfiles,
            IReadOnlyList<MutationCandidatePart> candidateParts,
            IReadOnlyCollection<string> equippedPartIds,
            int maxOptions,
            VariantScoringParameters scoring);
    }
}
