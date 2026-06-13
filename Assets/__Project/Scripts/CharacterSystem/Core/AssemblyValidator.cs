using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Stateless validation rules shared by the runtime factory (fail fast with clear
    /// messages) and the editor tooling (inspector warnings, preview window panel).
    /// </summary>
    public sealed class AssemblyValidator
    {
        public IReadOnlyList<ValidationIssue> ValidateSkeleton(SkeletonData skeleton)
        {
            if (skeleton == null)
            {
                throw new ArgumentNullException(nameof(skeleton));
            }

            var issues = new List<ValidationIssue>();

            var seenBones = new HashSet<string>(StringComparer.Ordinal);
            foreach (var boneName in skeleton.BoneNames)
            {
                if (!seenBones.Add(boneName))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        ValidationIssueCode.DuplicateBoneName,
                        boneName,
                        $"Skeleton '{skeleton.SkeletonId}' lists bone '{boneName}' more than once; name-based remapping becomes ambiguous."));
                }
            }

            var seenSockets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var socket in skeleton.Tier1Sockets)
            {
                if (!seenSockets.Add(socket.Id))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        ValidationIssueCode.DuplicateSocketId,
                        socket.Id,
                        $"Skeleton '{skeleton.SkeletonId}' defines Tier-1 socket '{socket.Id}' more than once."));
                }

                ValidateSocketParentBone(socket, skeleton, issues);
            }

            return issues;
        }

        public IReadOnlyList<ValidationIssue> ValidatePart(PartData part, SkeletonData skeleton)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (skeleton == null)
            {
                throw new ArgumentNullException(nameof(skeleton));
            }

            var issues = new List<ValidationIssue>();

            if (!string.Equals(part.TargetSkeletonId, skeleton.SkeletonId, StringComparison.Ordinal))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    ValidationIssueCode.SkeletonMismatch,
                    part.PartId,
                    $"Part '{part.PartId}' targets skeleton '{part.TargetSkeletonId}' but is being validated against '{skeleton.SkeletonId}'."));
            }

            if (part.BoneNames.Count == 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    ValidationIssueCode.EmptyBoneList,
                    part.PartId,
                    $"Part '{part.PartId}' has an empty bone-name list; bake it from the part prefab."));
            }

            foreach (var boneName in part.BoneNames)
            {
                if (!skeleton.HasBone(boneName))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        ValidationIssueCode.MissingBone,
                        part.PartId,
                        $"Part '{part.PartId}' is skinned to bone '{boneName}' which does not exist on skeleton '{skeleton.SkeletonId}'."));
                }
            }

            var seenSockets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var socket in part.ContributedSockets)
            {
                if (!seenSockets.Add(socket.Id))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        ValidationIssueCode.DuplicateSocketId,
                        socket.Id,
                        $"Part '{part.PartId}' contributes socket '{socket.Id}' more than once."));
                }

                ValidateSocketParentBone(socket, skeleton, issues);
            }

            return issues;
        }

        public IReadOnlyList<ValidationIssue> ValidateAssembly(SkeletonData skeleton, IReadOnlyList<PartData> parts)
        {
            if (skeleton == null)
            {
                throw new ArgumentNullException(nameof(skeleton));
            }

            var issues = new List<ValidationIssue>(ValidateSkeleton(skeleton));

            if (parts == null)
            {
                return issues;
            }

            var seenSlots = new HashSet<string>(StringComparer.Ordinal);
            var seenSocketIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var socket in skeleton.Tier1Sockets)
            {
                seenSocketIds.Add(socket.Id);
            }

            foreach (var part in parts)
            {
                if (part == null)
                {
                    continue;
                }

                issues.AddRange(ValidatePart(part, skeleton));

                if (!seenSlots.Add(part.SlotId))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        ValidationIssueCode.DuplicateSlot,
                        part.SlotId,
                        $"Assembly equips more than one part into slot '{part.SlotId}' (offending part: '{part.PartId}')."));
                }

                foreach (var socket in part.ContributedSockets)
                {
                    if (!seenSocketIds.Add(socket.Id))
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Warning,
                            ValidationIssueCode.DuplicateSocketId,
                            socket.Id,
                            $"Socket id '{socket.Id}' contributed by part '{part.PartId}' collides with an already-present socket; the first occurrence wins."));
                    }
                }
            }

            return issues;
        }

        private static void ValidateSocketParentBone(SocketInfo socket, SkeletonData skeleton, List<ValidationIssue> issues)
        {
            if (!skeleton.HasBone(socket.ParentBoneName))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    ValidationIssueCode.SocketParentBoneMissing,
                    socket.Id,
                    $"Socket '{socket.Id}' is parented to bone '{socket.ParentBoneName}' which does not exist on skeleton '{skeleton.SkeletonId}'."));
            }
        }
    }
}
