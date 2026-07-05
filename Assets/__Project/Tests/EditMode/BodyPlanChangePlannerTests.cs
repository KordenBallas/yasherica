using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BodyPlanChangePlannerTests
    {
        private const string BaseSkeletonId = "skeleton.placeholder";
        private const string SerpentSkeletonId = "skeleton.serpent";
        private const string SpiderSkeletonId = "skeleton.spider";

        private BodyPlanChangePlanner _planner;
        private Dictionary<string, SkeletonData> _skeletons;

        [SetUp]
        public void SetUp()
        {
            _planner = new BodyPlanChangePlanner();

            // Base biped: legs present. Serpent: legs gone, longer tail. Spider: legs gone,
            // radial spider bones. Shared bones keep identical names across frames.
            _skeletons = new Dictionary<string, SkeletonData>(StringComparer.Ordinal)
            {
                [BaseSkeletonId] = new SkeletonData(
                    BaseSkeletonId,
                    new[] { "Root", "Pelvis", "Spine", "Chest", "Head", "UpperLeg.L", "UpperLeg.R", "Tail.0" },
                    null),
                [SerpentSkeletonId] = new SkeletonData(
                    SerpentSkeletonId,
                    new[] { "Root", "Pelvis", "Spine", "Chest", "Head", "Tail.0", "Tail.1", "Tail.2" },
                    null),
                [SpiderSkeletonId] = new SkeletonData(
                    SpiderSkeletonId,
                    new[] { "Root", "Pelvis", "Spine", "Chest", "Head", "Tail.0", "SpiderHip.0", "SpiderHip.1" },
                    null)
            };
        }

        private SkeletonData Lookup(string skeletonId)
        {
            return _skeletons.TryGetValue(skeletonId, out var skeleton) ? skeleton : null;
        }

        private static PartData Part(string partId, string slotId, params string[] bones)
        {
            return new PartData(partId, slotId, BaseSkeletonId, bones, null);
        }

        private static PartData FrameChanger(
            string partId, string slotId, string targetSkeletonId, int priority, params string[] bones)
        {
            return new PartData(
                partId, slotId, targetSkeletonId, bones, null,
                governsBodyPlan: true, bodyPlanPriority: priority);
        }

        private static List<PartData> BaseBody()
        {
            return new List<PartData>
            {
                Part("part.head.a", "slot.head", "Head"),
                Part("part.torso.a", "slot.torso", "Spine", "Chest"),
                Part("part.leg.l", "slot.leg.l", "UpperLeg.L"),
                Part("part.leg.r", "slot.leg.r", "UpperLeg.R"),
                Part("part.tail.a", "slot.tail", "Tail.0")
            };
        }

        private static PartData SerpentSpine(int priority = 20)
        {
            return FrameChanger("part.spine.serpent", "slot.tail", SerpentSkeletonId, priority,
                "Pelvis", "Tail.0", "Tail.1", "Tail.2");
        }

        private static PartData SpiderLegs(int priority = 10)
        {
            return FrameChanger("part.legs.spider", "slot.legs.cluster", SpiderSkeletonId, priority,
                "Pelvis", "SpiderHip.0", "SpiderHip.1");
        }

        [Test]
        public void Plan_OrdinaryPartOnCurrentFrame_InstantSwap()
        {
            var incoming = Part("part.head.b", "slot.head", "Head");

            var plan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, BaseBody(), incoming, Lookup);

            Assert.AreEqual(BodyPlanChangeKind.InstantSwap, plan.Kind);
            Assert.AreEqual(BaseSkeletonId, plan.GoverningSkeletonId);
            Assert.IsFalse(plan.RequiresConfirmation);
            Assert.AreEqual(0, plan.ShedParts.Count);
        }

        [Test]
        public void Plan_OrdinaryPartNotFittingCurrentFrame_Incompatible()
        {
            var incoming = new PartData(
                "part.leg.serpentine", "slot.head", SerpentSkeletonId, new[] { "Tail.2" }, null);

            var plan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, BaseBody(), incoming, Lookup);

            Assert.AreEqual(BodyPlanChangeKind.Incompatible, plan.Kind);
        }

        [Test]
        public void Plan_SerpentInstallOnLeggedBase_ShedsExactlyBothLegs()
        {
            var plan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, BaseBody(), SerpentSpine(), Lookup);

            Assert.AreEqual(BodyPlanChangeKind.FrameChange, plan.Kind);
            Assert.AreEqual(SerpentSkeletonId, plan.GoverningSkeletonId);
            Assert.AreEqual("part.spine.serpent", plan.GoverningPartId);
            Assert.IsTrue(plan.RequiresConfirmation);

            CollectionAssert.AreEqual(
                new[] { "part.leg.l", "part.leg.r" },
                plan.ShedParts.Select(p => p.PartId).ToArray());

            // Head + torso survive structurally; the serpent spine replaced the tail slot occupant.
            CollectionAssert.AreEquivalent(
                new[] { "part.head.a", "part.torso.a", "part.spine.serpent" },
                plan.ActiveParts.Select(p => p.PartId).ToArray());
            Assert.AreEqual(0, plan.DormantParts.Count);
        }

        [Test]
        public void Plan_FrameChangersNeverShed_LoserGoesDormant()
        {
            var body = BaseBody();
            body.RemoveAll(p => p.SlotId == "slot.tail");
            body.Add(SpiderLegs());
            // Spider currently governs; installing the higher-priority serpent takes over.
            var plan = _planner.Plan(SpiderSkeletonId, BaseSkeletonId, body, SerpentSpine(), Lookup);

            Assert.AreEqual(BodyPlanChangeKind.FrameChange, plan.Kind);
            Assert.AreEqual(SerpentSkeletonId, plan.GoverningSkeletonId);
            CollectionAssert.AreEqual(
                new[] { "part.legs.spider" },
                plan.DormantParts.Select(p => p.PartId).ToArray());
            Assert.IsFalse(plan.ShedParts.Any(p => p.GovernsBodyPlan));
        }

        [Test]
        public void Plan_LowerPriorityGovernorArrives_DormantInstallNoPrompt()
        {
            var body = BaseBody();
            body.RemoveAll(p => p.SlotId == "slot.tail" || p.SlotId == "slot.leg.l" || p.SlotId == "slot.leg.r");
            body.Add(SerpentSpine());

            var plan = _planner.Plan(SerpentSkeletonId, BaseSkeletonId, body, SpiderLegs(), Lookup);

            Assert.AreEqual(BodyPlanChangeKind.DormantInstall, plan.Kind);
            Assert.AreEqual(SerpentSkeletonId, plan.GoverningSkeletonId);
            Assert.IsFalse(plan.RequiresConfirmation);
        }

        [Test]
        public void Plan_ReplacingWinnerSlot_GovernanceFallsToDormantGovernor()
        {
            // Serpent governs; spider waits dormant. A plain tail part replaces the serpent spine.
            var body = new List<PartData>
            {
                Part("part.head.a", "slot.head", "Head"),
                Part("part.torso.a", "slot.torso", "Spine", "Chest"),
                SerpentSpine(),
                SpiderLegs()
            };
            var incoming = Part("part.tail.a", "slot.tail", "Tail.0");

            var plan = _planner.Plan(SerpentSkeletonId, BaseSkeletonId, body, incoming, Lookup);

            Assert.AreEqual(BodyPlanChangeKind.FrameChange, plan.Kind);
            Assert.AreEqual(SpiderSkeletonId, plan.GoverningSkeletonId);
            Assert.AreEqual("part.legs.spider", plan.GoverningPartId);
            // The plain tail fits the spider frame (Tail.0 exists there).
            Assert.IsTrue(plan.ActiveParts.Any(p => p.PartId == "part.tail.a"));
        }

        [Test]
        public void Plan_FrameChangeSheddingNothing_NeedsNoConfirmation()
        {
            var body = new List<PartData>
            {
                Part("part.head.a", "slot.head", "Head"),
                Part("part.torso.a", "slot.torso", "Spine", "Chest")
            };

            var plan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, body, SerpentSpine(), Lookup);

            Assert.AreEqual(BodyPlanChangeKind.FrameChange, plan.Kind);
            Assert.AreEqual(0, plan.ShedParts.Count);
            Assert.IsFalse(plan.RequiresConfirmation);
        }

        [Test]
        public void Plan_PartWithOrphanedContributedSocket_Sheds()
        {
            var body = BaseBody();
            // A torso variant whose contributed socket hangs off a leg bone: its skinned bones
            // survive the serpent frame but the socket parent does not -> it must shed.
            body.RemoveAll(p => p.SlotId == "slot.torso");
            body.Add(new PartData(
                "part.torso.legmounted", "slot.torso", BaseSkeletonId,
                new[] { "Spine", "Chest" },
                new[] { new SocketInfo("socket.knee", "UpperLeg.L", SocketTier.Part, "part.torso.legmounted") }));

            var plan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, body, SerpentSpine(), Lookup);

            Assert.IsTrue(plan.ShedParts.Any(p => p.PartId == "part.torso.legmounted"));
        }

        [Test]
        public void Plan_ShedListSortedBySlotId_AndInputOrderIrrelevant()
        {
            var body = BaseBody();
            var shuffled = new List<PartData> { body[4], body[2], body[0], body[3], body[1] };

            var plan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, body, SerpentSpine(), Lookup);
            var shuffledPlan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, shuffled, SerpentSpine(), Lookup);

            CollectionAssert.AreEqual(
                plan.ShedParts.Select(p => p.PartId).ToArray(),
                shuffledPlan.ShedParts.Select(p => p.PartId).ToArray());
            CollectionAssert.AreEqual(
                plan.ActiveParts.Select(p => p.PartId).ToArray(),
                shuffledPlan.ActiveParts.Select(p => p.PartId).ToArray());
            CollectionAssert.AreEqual(
                plan.ShedParts.Select(p => p.SlotId).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                plan.ShedParts.Select(p => p.SlotId).ToArray());
        }

        [Test]
        public void Plan_GoverningSkeletonUnknown_Incompatible()
        {
            var incoming = FrameChanger("part.frame.lost", "slot.tail", "skeleton.unknown", 99, "Pelvis");

            var plan = _planner.Plan(BaseSkeletonId, BaseSkeletonId, BaseBody(), incoming, Lookup);

            Assert.AreEqual(BodyPlanChangeKind.Incompatible, plan.Kind);
        }

        [Test]
        public void Fits_MissingSkinnedBone_False()
        {
            Assert.IsFalse(BodyPlanChangePlanner.Fits(
                Part("part.leg.l", "slot.leg.l", "UpperLeg.L"),
                _skeletons[SerpentSkeletonId]));
        }

        [Test]
        public void Fits_AllBonesAndSocketParentsResolve_True()
        {
            var part = new PartData(
                "part.head.a", "slot.head", BaseSkeletonId,
                new[] { "Head" },
                new[] { new SocketInfo("socket.hat", "Head", SocketTier.Part, "part.head.a") });

            Assert.IsTrue(BodyPlanChangePlanner.Fits(part, _skeletons[SerpentSkeletonId]));
        }
    }
}
