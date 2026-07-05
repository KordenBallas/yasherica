using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BodyPlanResolverTests
    {
        private const string BaseSkeletonId = "skeleton.placeholder";

        private BodyPlanResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new BodyPlanResolver();
        }

        private static PartData OrdinaryPart(string partId, string slotId)
        {
            return new PartData(partId, slotId, BaseSkeletonId, new[] { "Spine" }, null);
        }

        private static PartData FrameChanger(string partId, string slotId, string targetSkeletonId, int priority)
        {
            return new PartData(
                partId, slotId, targetSkeletonId, new[] { "Spine" }, null,
                governsBodyPlan: true, bodyPlanPriority: priority);
        }

        [Test]
        public void Resolve_NoGovernors_BasePlanGoverns()
        {
            var equipped = new[] { OrdinaryPart("part.head.a", "slot.head"), OrdinaryPart("part.torso.a", "slot.torso") };

            var resolution = _resolver.Resolve(BaseSkeletonId, equipped);

            Assert.AreEqual(BaseSkeletonId, resolution.GoverningSkeletonId);
            Assert.IsNull(resolution.GoverningPartId);
            Assert.IsTrue(resolution.IsBasePlan);
        }

        [Test]
        public void Resolve_EmptySet_BasePlanGoverns()
        {
            var resolution = _resolver.Resolve(BaseSkeletonId, new List<PartData>());

            Assert.AreEqual(BaseSkeletonId, resolution.GoverningSkeletonId);
            Assert.IsTrue(resolution.IsBasePlan);
        }

        [Test]
        public void Resolve_SingleGovernor_ItsSkeletonGoverns()
        {
            var equipped = new[]
            {
                OrdinaryPart("part.head.a", "slot.head"),
                FrameChanger("part.spine.serpent", "slot.tail", "skeleton.serpent", 20)
            };

            var resolution = _resolver.Resolve(BaseSkeletonId, equipped);

            Assert.AreEqual("skeleton.serpent", resolution.GoverningSkeletonId);
            Assert.AreEqual("part.spine.serpent", resolution.GoverningPartId);
            Assert.IsFalse(resolution.IsBasePlan);
        }

        [Test]
        public void Resolve_TwoGovernors_HigherPriorityWinsRegardlessOfOrder()
        {
            var serpent = FrameChanger("part.spine.serpent", "slot.tail", "skeleton.serpent", 20);
            var spider = FrameChanger("part.legs.spider", "slot.legs.cluster", "skeleton.spider", 10);

            var forward = _resolver.Resolve(BaseSkeletonId, new[] { serpent, spider });
            var reversed = _resolver.Resolve(BaseSkeletonId, new[] { spider, serpent });

            Assert.AreEqual("skeleton.serpent", forward.GoverningSkeletonId);
            Assert.AreEqual(forward.GoverningSkeletonId, reversed.GoverningSkeletonId);
            Assert.AreEqual(forward.GoverningPartId, reversed.GoverningPartId);
        }

        [Test]
        public void Resolve_EqualPriority_TieBreaksByOrdinalPartIdAscending()
        {
            var a = FrameChanger("part.frame.a", "slot.tail", "skeleton.a", 10);
            var b = FrameChanger("part.frame.b", "slot.legs.cluster", "skeleton.b", 10);

            var forward = _resolver.Resolve(BaseSkeletonId, new[] { a, b });
            var reversed = _resolver.Resolve(BaseSkeletonId, new[] { b, a });

            Assert.AreEqual("part.frame.a", forward.GoverningPartId);
            Assert.AreEqual("part.frame.a", reversed.GoverningPartId);
        }

        [Test]
        public void Resolve_WinnerAbsent_NextHighestRemainingGovernorWins()
        {
            var spider = FrameChanger("part.legs.spider", "slot.legs.cluster", "skeleton.spider", 10);

            var resolution = _resolver.Resolve(BaseSkeletonId, new[] { OrdinaryPart("part.head.a", "slot.head"), spider });

            Assert.AreEqual("skeleton.spider", resolution.GoverningSkeletonId);
            Assert.AreEqual("part.legs.spider", resolution.GoverningPartId);
        }

        [Test]
        public void Resolve_GovernorWithoutTargetSkeleton_IsIgnored()
        {
            var broken = new PartData(
                "part.frame.broken", "slot.tail", null, new[] { "Spine" }, null,
                governsBodyPlan: true, bodyPlanPriority: 99);

            var resolution = _resolver.Resolve(BaseSkeletonId, new[] { broken });

            Assert.IsTrue(resolution.IsBasePlan);
        }

        [Test]
        public void Resolve_RepeatedCalls_SameResult()
        {
            var equipped = new[]
            {
                FrameChanger("part.spine.serpent", "slot.tail", "skeleton.serpent", 20),
                FrameChanger("part.legs.spider", "slot.legs.cluster", "skeleton.spider", 10),
                OrdinaryPart("part.head.a", "slot.head")
            };

            var results = Enumerable.Range(0, 5)
                .Select(_ => _resolver.Resolve(BaseSkeletonId, equipped))
                .ToArray();

            Assert.IsTrue(results.All(r => r.GoverningSkeletonId == "skeleton.serpent"));
            Assert.IsTrue(results.All(r => r.GoverningPartId == "part.spine.serpent"));
        }
    }
}
