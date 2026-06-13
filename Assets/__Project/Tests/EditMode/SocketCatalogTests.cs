using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class SocketCatalogTests
    {
        private const string SkeletonId = "skeleton.placeholder";

        private SocketCatalog _catalog;
        private SkeletonData _skeleton;

        [SetUp]
        public void SetUp()
        {
            _catalog = new SocketCatalog();
            _skeleton = new SkeletonData(
                SkeletonId,
                new[] { "Root", "Hand.L", "Hand.R", "Chest", "Tail.2" },
                new[]
                {
                    new SocketInfo("socket.palm.l", "Hand.L", SocketTier.Skeleton),
                    new SocketInfo("socket.palm.r", "Hand.R", SocketTier.Skeleton),
                    new SocketInfo("socket.back", "Chest", SocketTier.Skeleton)
                });
        }

        private static PartData Part(string partId, string slotId, params SocketInfo[] sockets)
        {
            return new PartData(partId, slotId, SkeletonId, new[] { "Root" }, sockets);
        }

        [Test]
        public void Rebuild_WithNoParts_ContainsOnlyTier1Sockets()
        {
            _catalog.Rebuild(_skeleton, new List<PartData>());

            Assert.AreEqual(3, _catalog.AvailableSockets.Count);
            Assert.IsTrue(_catalog.Contains("socket.palm.l"));
            Assert.IsTrue(_catalog.Contains("socket.palm.r"));
            Assert.IsTrue(_catalog.Contains("socket.back"));
        }

        [Test]
        public void Rebuild_WithContributingPart_AddsTier2Sockets()
        {
            var torso = Part("part.torso.a", "slot.torso",
                new SocketInfo("socket.wing.l", "Chest", SocketTier.Part, "part.torso.a"));

            _catalog.Rebuild(_skeleton, new[] { torso });

            Assert.AreEqual(4, _catalog.AvailableSockets.Count);
            Assert.IsTrue(_catalog.TryGet("socket.wing.l", out var socket));
            Assert.AreEqual(SocketTier.Part, socket.Tier);
            Assert.AreEqual("part.torso.a", socket.SourcePartId);
        }

        [Test]
        public void Rebuild_AfterPartRemoved_Tier2SocketsDisappear()
        {
            var torso = Part("part.torso.a", "slot.torso",
                new SocketInfo("socket.wing.l", "Chest", SocketTier.Part, "part.torso.a"));

            _catalog.Rebuild(_skeleton, new[] { torso });
            _catalog.Rebuild(_skeleton, new List<PartData>());

            Assert.AreEqual(3, _catalog.AvailableSockets.Count);
            Assert.IsFalse(_catalog.Contains("socket.wing.l"));
        }

        [Test]
        public void Rebuild_SwapBetweenParts_DiffsOldAndNewSockets()
        {
            var torsoA = Part("part.torso.a", "slot.torso",
                new SocketInfo("socket.wing.l", "Chest", SocketTier.Part, "part.torso.a"));
            var torsoB = Part("part.torso.b", "slot.torso",
                new SocketInfo("socket.cape", "Chest", SocketTier.Part, "part.torso.b"));

            _catalog.Rebuild(_skeleton, new[] { torsoA });

            IReadOnlyList<SocketInfo> added = null;
            IReadOnlyList<SocketInfo> removed = null;
            _catalog.Changed += (a, r) => { added = a; removed = r; };

            _catalog.Rebuild(_skeleton, new[] { torsoB });

            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual("socket.wing.l", removed[0].Id);
            Assert.AreEqual(1, added.Count);
            Assert.AreEqual("socket.cape", added[0].Id);
            Assert.IsFalse(_catalog.Contains("socket.wing.l"));
            Assert.IsTrue(_catalog.Contains("socket.cape"));
        }

        [Test]
        public void Rebuild_SameSocketIdFromDifferentPart_ReportedAsRemovedAndAdded()
        {
            var headA = Part("part.head.a", "slot.head",
                new SocketInfo("socket.hat", "Root", SocketTier.Part, "part.head.a"));
            var headB = Part("part.head.b", "slot.head",
                new SocketInfo("socket.hat", "Root", SocketTier.Part, "part.head.b"));

            _catalog.Rebuild(_skeleton, new[] { headA });

            IReadOnlyList<SocketInfo> added = null;
            IReadOnlyList<SocketInfo> removed = null;
            _catalog.Changed += (a, r) => { added = a; removed = r; };

            _catalog.Rebuild(_skeleton, new[] { headB });

            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual("part.head.a", removed[0].SourcePartId);
            Assert.AreEqual(1, added.Count);
            Assert.AreEqual("part.head.b", added[0].SourcePartId);
        }

        [Test]
        public void Rebuild_NoEffectiveChange_DoesNotRaiseChanged()
        {
            var torso = Part("part.torso.a", "slot.torso",
                new SocketInfo("socket.wing.l", "Chest", SocketTier.Part, "part.torso.a"));

            _catalog.Rebuild(_skeleton, new[] { torso });

            var raised = false;
            _catalog.Changed += (a, r) => raised = true;

            _catalog.Rebuild(_skeleton, new[] { torso });

            Assert.IsFalse(raised);
        }

        [Test]
        public void Rebuild_DuplicateSocketIds_FirstOccurrenceWins()
        {
            var rogue = Part("part.rogue", "slot.torso",
                new SocketInfo("socket.back", "Tail.2", SocketTier.Part, "part.rogue"));

            _catalog.Rebuild(_skeleton, new[] { rogue });

            Assert.AreEqual(3, _catalog.AvailableSockets.Count);
            Assert.IsTrue(_catalog.TryGet("socket.back", out var socket));
            Assert.AreEqual(SocketTier.Skeleton, socket.Tier);
            Assert.AreEqual("Chest", socket.ParentBoneName);
        }

        [Test]
        public void AvailableSockets_PreservesTier1ThenPartOrder()
        {
            var tail = Part("part.tail", "slot.tail",
                new SocketInfo("socket.tail.tip", "Tail.2", SocketTier.Part, "part.tail"));

            _catalog.Rebuild(_skeleton, new[] { tail });

            var ids = _catalog.AvailableSockets.Select(s => s.Id).ToArray();
            CollectionAssert.AreEqual(
                new[] { "socket.palm.l", "socket.palm.r", "socket.back", "socket.tail.tip" },
                ids);
        }
    }
}
