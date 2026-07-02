using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Deterministically picks the authored artifact that best expresses a target
    /// trait profile: shared traits score, off-target traits and tier distance
    /// penalize, ties break by ordinal definition id. The argmax over the whole
    /// authored pool is total, which is what structurally guarantees the fusion
    /// layer's no-failure rule. Inputs are excluded so an emergent combine yields
    /// something new - unless exclusion would empty the pool.
    /// </summary>
    public class ArtifactByTraitSelector
    {
        /// <summary>Returns the best definition id, or null only when the source is empty.</summary>
        public string SelectBest(
            ArtifactTraitProfile target,
            IArtifactTraitSource source,
            IReadOnlyCollection<string> excludedDefinitionIds,
            FusionSettings settings)
        {
            string best = SelectBestInternal(target, source, excludedDefinitionIds, settings);
            if (best == null)
            {
                // Every candidate was excluded (e.g. the inputs are the entire pool);
                // yielding an input back beats yielding nothing.
                best = SelectBestInternal(target, source, null, settings);
            }

            return best;
        }

        private static string SelectBestInternal(
            ArtifactTraitProfile target,
            IArtifactTraitSource source,
            IReadOnlyCollection<string> excludedDefinitionIds,
            FusionSettings settings)
        {
            string bestId = null;
            float bestScore = float.MinValue;

            foreach (var entry in source.All)
            {
                if (entry.Profile == null || string.IsNullOrEmpty(entry.DefinitionId))
                {
                    continue;
                }

                if (excludedDefinitionIds != null && Contains(excludedDefinitionIds, entry.DefinitionId))
                {
                    continue;
                }

                float score = Score(target, entry.Profile, settings);
                if (score > bestScore ||
                    (score == bestScore && string.CompareOrdinal(entry.DefinitionId, bestId) < 0))
                {
                    bestScore = score;
                    bestId = entry.DefinitionId;
                }
            }

            return bestId;
        }

        private static float Score(
            ArtifactTraitProfile target,
            ArtifactTraitProfile candidate,
            FusionSettings settings)
        {
            int overlap = 0;
            int offTarget = 0;

            foreach (var trait in candidate.Traits)
            {
                if (target.Has(trait))
                {
                    overlap++;
                }
                else
                {
                    offTarget++;
                }
            }

            int tierDistance = target.Tier > candidate.Tier
                ? target.Tier - candidate.Tier
                : candidate.Tier - target.Tier;

            return overlap * settings.TraitOverlapWeight
                   - offTarget * settings.TraitMismatchWeight
                   - tierDistance * settings.TierProximityWeight;
        }

        private static bool Contains(IReadOnlyCollection<string> ids, string id)
        {
            foreach (var candidate in ids)
            {
                if (candidate == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
