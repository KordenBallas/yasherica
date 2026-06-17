using System.Collections.Generic;
using Mutation.Core;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Source of every body part the stage-up mutation choice can score, as UnityEngine-free
    /// <see cref="MutationCandidatePart"/> records (so the scoring stays pure C#), plus the choice
    /// icon authored on each part. Built once from the character <c>IPartCatalog</c>; replaces the
    /// old archetype-keyed option provider now that affinity/rarity/icon live on the part itself.
    /// </summary>
    public interface IMutationPartCatalog
    {
        /// <summary>Every candidate part, in catalog order. Never null.</summary>
        IReadOnlyList<MutationCandidatePart> AllCandidates { get; }

        /// <summary>The icon authored on <paramref name="partId"/>'s part, if any.</summary>
        bool TryGetIcon(string partId, out Sprite icon);
    }
}
