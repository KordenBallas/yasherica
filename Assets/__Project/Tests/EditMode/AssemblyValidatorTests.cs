using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class AssemblyValidatorTests
    {
        private const string SkeletonId = "skeleton.placeholder";

        private AssemblyValidator _validator;
        private SkeletonData _skeleton;

        [SetUp]
        public void SetUp()
        {
            _validator = new AssemblyValidator();
            _skeleton = new SkeletonData(
                SkeletonId,
                new[] { "Root", "Pelvis", "Spine", "Chest", "Head" },
                new[]
                {
                    new SocketInfo("socket.back", "Chest", SocketTier.Skeleton)
                });
        }

        private static PartData Part(
            string partId,
            string slotId,
            string targetSkeletonId = SkeletonId,
            IReadOnlyList<string> boneNames = null,
            params SocketInfo[] sockets)
        {
            return new PartData(partId, slotId, targetSkeletonId, boneNames ?? new[] { "Spine" }, sockets);
        }

        [Test]
        public void ValidatePart_CleanPart_NoIssues()
        {
            var issues = _validator.ValidatePart(Part("part.torso.a", "slot.torso"), _skeleton);

            Assert.AreEqual(0, issues.Count);
        }

        [Test]
        public void ValidatePart_WrongTargetSkeleton_ReportsError()
        {
            var part = Part("part.alien", "slot.torso", targetSkeletonId: "skeleton.other");

            var issues = _validator.ValidatePart(part, _skeleton);

            var issue = issues.Single(i => i.Code == ValidationIssueCode.SkeletonMismatch);
            Assert.AreEqual(ValidationSeverity.Error, issue.Severity);
            Assert.AreEqual("part.alien", issue.SubjectId);
        }

        [Test]
        public void ValidatePart_MissingBone_ReportsErrorPerBone()
        {
            var part = Part("part.torso.a", "slot.torso", boneNames: new[] { "Spine", "Tail.0", "Wing.L" });

            var issues = _validator.ValidatePart(part, _skeleton);

            var missing = issues.Where(i => i.Code == ValidationIssueCode.MissingBone).ToArray();
            Assert.AreEqual(2, missing.Length);
            Assert.IsTrue(missing.All(i => i.Severity == ValidationSeverity.Error));
        }

        [Test]
        public void ValidatePart_EmptyBoneList_ReportsError()
        {
            var part = Part("part.hollow", "slot.torso", boneNames: Array.Empty<string>());

            var issues = _validator.ValidatePart(part, _skeleton);

            Assert.IsTrue(issues.Any(i =>
                i.Code == ValidationIssueCode.EmptyBoneList && i.Severity == ValidationSeverity.Error));
        }

        [Test]
        public void ValidatePart_SocketParentBoneMissing_ReportsError()
        {
            var part = Part("part.torso.a", "slot.torso", sockets:
                new SocketInfo("socket.wing.l", "Wing.L", SocketTier.Part, "part.torso.a"));

            var issues = _validator.ValidatePart(part, _skeleton);

            var issue = issues.Single(i => i.Code == ValidationIssueCode.SocketParentBoneMissing);
            Assert.AreEqual(ValidationSeverity.Error, issue.Severity);
            Assert.AreEqual("socket.wing.l", issue.SubjectId);
        }

        [Test]
        public void ValidateAssembly_TwoPartsInSameSlot_ReportsError()
        {
            var parts = new[]
            {
                Part("part.torso.a", "slot.torso"),
                Part("part.torso.b", "slot.torso")
            };

            var issues = _validator.ValidateAssembly(_skeleton, parts);

            var issue = issues.Single(i => i.Code == ValidationIssueCode.DuplicateSlot);
            Assert.AreEqual(ValidationSeverity.Error, issue.Severity);
            Assert.AreEqual("slot.torso", issue.SubjectId);
        }

        [Test]
        public void ValidateAssembly_SocketIdCollisionAcrossParts_ReportsWarning()
        {
            var parts = new[]
            {
                Part("part.head.a", "slot.head", sockets:
                    new SocketInfo("socket.hat", "Head", SocketTier.Part, "part.head.a")),
                Part("part.torso.a", "slot.torso", sockets:
                    new SocketInfo("socket.hat", "Chest", SocketTier.Part, "part.torso.a"))
            };

            var issues = _validator.ValidateAssembly(_skeleton, parts);

            var issue = issues.Single(i => i.Code == ValidationIssueCode.DuplicateSocketId);
            Assert.AreEqual(ValidationSeverity.Warning, issue.Severity);
            Assert.AreEqual("socket.hat", issue.SubjectId);
        }

        [Test]
        public void ValidateAssembly_CleanAssembly_NoIssues()
        {
            var parts = new[]
            {
                Part("part.head.a", "slot.head", boneNames: new[] { "Head" }, sockets:
                    new SocketInfo("socket.hat", "Head", SocketTier.Part, "part.head.a")),
                Part("part.torso.a", "slot.torso", boneNames: new[] { "Spine", "Chest" })
            };

            var issues = _validator.ValidateAssembly(_skeleton, parts);

            Assert.AreEqual(0, issues.Count);
        }

        [Test]
        public void ValidateSkeleton_DuplicateTier1SocketId_ReportsError()
        {
            var skeleton = new SkeletonData(
                SkeletonId,
                new[] { "Root", "Chest" },
                new[]
                {
                    new SocketInfo("socket.back", "Chest", SocketTier.Skeleton),
                    new SocketInfo("socket.back", "Root", SocketTier.Skeleton)
                });

            var issues = _validator.ValidateSkeleton(skeleton);

            Assert.IsTrue(issues.Any(i =>
                i.Code == ValidationIssueCode.DuplicateSocketId && i.Severity == ValidationSeverity.Error));
        }
    }
}
