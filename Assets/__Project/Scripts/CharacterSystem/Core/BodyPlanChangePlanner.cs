using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// The one pure decision function of the body-plan system: given the current body and an
    /// incoming part, computes what installing it would do (see <see cref="BodyPlanChangeKind"/>)
    /// and, on a frame change, classifies every part of the prospective body as active,
    /// dormant, or shed. Structural fit — every skinned bone and every contributed socket's
    /// parent bone resolves on the skeleton — is the single compatibility test (FR4: fit is
    /// checked, never assumed from authoring provenance).
    /// </summary>
    public sealed class BodyPlanChangePlanner
    {
        private readonly BodyPlanResolver _resolver = new BodyPlanResolver();

        /// <summary>
        /// Plans the install of <paramref name="incomingPart"/> onto a body currently on
        /// <paramref name="currentSkeletonId"/>. <paramref name="equippedParts"/> must include
        /// dormant frame-changers (they stay governance candidates). The prospective body is
        /// the equipped set with the incoming part replacing any same-slot occupant.
        /// </summary>
        public BodyPlanChangePlan Plan(
            string currentSkeletonId,
            string baseSkeletonId,
            IReadOnlyList<PartData> equippedParts,
            PartData incomingPart,
            Func<string, SkeletonData> skeletonLookup)
        {
            if (incomingPart == null)
            {
                throw new ArgumentNullException(nameof(incomingPart));
            }

            if (skeletonLookup == null)
            {
                throw new ArgumentNullException(nameof(skeletonLookup));
            }

            var prospective = BuildProspectiveSet(equippedParts, incomingPart);
            var resolution = _resolver.Resolve(baseSkeletonId, prospective);

            if (string.Equals(resolution.GoverningSkeletonId, currentSkeletonId, StringComparison.Ordinal))
            {
                return PlanWithinCurrentFrame(currentSkeletonId, incomingPart, resolution, skeletonLookup);
            }

            return PlanFrameChange(prospective, resolution, skeletonLookup);
        }

        /// <summary>Structural fit: every skinned bone and every contributed socket's parent
        /// bone exists on the skeleton. The single source of truth for "has an attach point".</summary>
        public static bool Fits(PartData part, SkeletonData skeleton)
        {
            if (part == null || skeleton == null || part.BoneNames.Count == 0)
            {
                return false;
            }

            foreach (var boneName in part.BoneNames)
            {
                if (!skeleton.HasBone(boneName))
                {
                    return false;
                }
            }

            foreach (var socket in part.ContributedSockets)
            {
                if (!skeleton.HasBone(socket.ParentBoneName))
                {
                    return false;
                }
            }

            return true;
        }

        private BodyPlanChangePlan PlanWithinCurrentFrame(
            string currentSkeletonId,
            PartData incomingPart,
            BodyPlanResolution resolution,
            Func<string, SkeletonData> skeletonLookup)
        {
            var isLosingGovernor = incomingPart.GovernsBodyPlan
                && !string.Equals(resolution.GoverningPartId, incomingPart.PartId, StringComparison.Ordinal);
            if (isLosingGovernor)
            {
                return new BodyPlanChangePlan(
                    BodyPlanChangeKind.DormantInstall,
                    resolution.GoverningSkeletonId,
                    resolution.GoverningPartId);
            }

            var currentSkeleton = skeletonLookup(currentSkeletonId);
            if (currentSkeleton == null || !Fits(incomingPart, currentSkeleton))
            {
                return new BodyPlanChangePlan(
                    BodyPlanChangeKind.Incompatible,
                    resolution.GoverningSkeletonId,
                    resolution.GoverningPartId);
            }

            return new BodyPlanChangePlan(
                BodyPlanChangeKind.InstantSwap,
                resolution.GoverningSkeletonId,
                resolution.GoverningPartId);
        }

        private static BodyPlanChangePlan PlanFrameChange(
            List<PartData> prospective,
            BodyPlanResolution resolution,
            Func<string, SkeletonData> skeletonLookup)
        {
            var newSkeleton = skeletonLookup(resolution.GoverningSkeletonId);
            if (newSkeleton == null)
            {
                // A governing part pulling in an unknown skeleton is an authoring error;
                // refuse the install rather than tearing down the body.
                return new BodyPlanChangePlan(
                    BodyPlanChangeKind.Incompatible,
                    resolution.GoverningSkeletonId,
                    resolution.GoverningPartId);
            }

            var active = new List<PartData>();
            var dormant = new List<PartData>();
            var shed = new List<PartData>();

            foreach (var part in prospective)
            {
                var isWinner = string.Equals(part.PartId, resolution.GoverningPartId, StringComparison.Ordinal);
                if (part.GovernsBodyPlan && !isWinner)
                {
                    // Losing frame-changers are never shed: they stay governance candidates
                    // so removing the winner later hands the body to the next-highest (FR2).
                    dormant.Add(part);
                }
                else if (Fits(part, newSkeleton))
                {
                    active.Add(part);
                }
                else
                {
                    shed.Add(part);
                }
            }

            SortBySlotId(active);
            SortBySlotId(dormant);
            SortBySlotId(shed);

            return new BodyPlanChangePlan(
                BodyPlanChangeKind.FrameChange,
                resolution.GoverningSkeletonId,
                resolution.GoverningPartId,
                active,
                dormant,
                shed);
        }

        private static List<PartData> BuildProspectiveSet(IReadOnlyList<PartData> equippedParts, PartData incomingPart)
        {
            var prospective = new List<PartData>();
            if (equippedParts != null)
            {
                foreach (var part in equippedParts)
                {
                    if (part != null && !string.Equals(part.SlotId, incomingPart.SlotId, StringComparison.Ordinal))
                    {
                        prospective.Add(part);
                    }
                }
            }

            prospective.Add(incomingPart);
            return prospective;
        }

        private static void SortBySlotId(List<PartData> parts)
        {
            parts.Sort((a, b) => string.CompareOrdinal(a.SlotId, b.SlotId));
        }
    }
}
