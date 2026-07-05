using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Resolves which body plan governs a set of equipped parts. A pure function of the
    /// equipped set (never of install order): among parts with
    /// <see cref="PartData.GovernsBodyPlan"/>, the highest <see cref="PartData.BodyPlanPriority"/>
    /// wins; ties break by ordinal <see cref="PartData.PartId"/> ascending; with no governor
    /// the base skeleton applies. Determinism here backs body-plan-skeleton-swap.md FR3/FR11.
    /// </summary>
    public sealed class BodyPlanResolver
    {
        public BodyPlanResolution Resolve(string baseSkeletonId, IEnumerable<PartData> equippedParts)
        {
            if (string.IsNullOrEmpty(baseSkeletonId))
            {
                throw new ArgumentException("Base skeleton id must be a non-empty string.", nameof(baseSkeletonId));
            }

            PartData winner = null;
            if (equippedParts != null)
            {
                foreach (var part in equippedParts)
                {
                    if (part == null || !part.GovernsBodyPlan || string.IsNullOrEmpty(part.TargetSkeletonId))
                    {
                        continue;
                    }

                    if (winner == null || Beats(part, winner))
                    {
                        winner = part;
                    }
                }
            }

            return winner == null
                ? new BodyPlanResolution(baseSkeletonId, null)
                : new BodyPlanResolution(winner.TargetSkeletonId, winner.PartId);
        }

        private static bool Beats(PartData candidate, PartData incumbent)
        {
            if (candidate.BodyPlanPriority != incumbent.BodyPlanPriority)
            {
                return candidate.BodyPlanPriority > incumbent.BodyPlanPriority;
            }

            return string.CompareOrdinal(candidate.PartId, incumbent.PartId) < 0;
        }
    }
}
