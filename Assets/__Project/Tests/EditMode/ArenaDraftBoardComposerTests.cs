using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Deterministic board composition (P4-5 reqs 5–7): same seed + same catalogs → the same
    /// board; empty catalogs degrade to a floor-only board; the catalog sample respects its
    /// size; composition is independent of input ordering.
    /// </summary>
    [TestFixture]
    public class ArenaDraftBoardComposerTests
    {
        private static readonly string[] Loadout = { "slot.head", "slot.torso" };

        private static ArenaDraftPartInfo Part(string partId, string slotId) =>
            new ArenaDraftPartInfo(partId, slotId);

        private static List<ArenaDraftPartInfo> Floor() => new List<ArenaDraftPartInfo>
        {
            Part("part.head.a", "slot.head"),
            Part("part.head.a", "slot.head"),
            Part("part.torso.a", "slot.torso"),
            Part("part.torso.a", "slot.torso")
        };

        private static List<ArenaDraftPartInfo> Catalog() => new List<ArenaDraftPartInfo>
        {
            Part("part.head.x", "slot.head"),
            Part("part.head.y", "slot.head"),
            Part("part.torso.x", "slot.torso"),
            Part("part.torso.y", "slot.torso")
        };

        [Test]
        public void Compose_SameSeedAndInputs_ProducesIdenticalBoards()
        {
            var first = ArenaDraftBoardComposer.Compose(42, Floor(), Catalog(), Loadout, 2);
            var second = ArenaDraftBoardComposer.Compose(42, Floor(), Catalog(), Loadout, 2);

            Assert.AreEqual(
                first.Select(e => (e.EntryId, e.PartId, e.SlotId)).ToList(),
                second.Select(e => (e.EntryId, e.PartId, e.SlotId)).ToList());
        }

        [Test]
        public void Compose_IsIndependentOfInputOrder()
        {
            var reversedFloor = Floor();
            reversedFloor.Reverse();
            var reversedCatalog = Catalog();
            reversedCatalog.Reverse();

            var first = ArenaDraftBoardComposer.Compose(42, Floor(), Catalog(), Loadout, 2);
            var second = ArenaDraftBoardComposer.Compose(42, reversedFloor, reversedCatalog, Loadout, 2);

            Assert.AreEqual(
                first.Select(e => (e.EntryId, e.PartId, e.SlotId)).ToList(),
                second.Select(e => (e.EntryId, e.PartId, e.SlotId)).ToList());
        }

        [Test]
        public void Compose_EmptyCatalog_YieldsFloorOnlyBoard()
        {
            var board = ArenaDraftBoardComposer.Compose(
                7, Floor(), new List<ArenaDraftPartInfo>(), Loadout, 3);

            Assert.AreEqual(Floor().Count, board.Count);
            Assert.IsTrue(board.All(e => e.PartId.EndsWith(".a")));
        }

        [Test]
        public void Compose_FloorInstancesAreAllStocked_AsSeparateEntries()
        {
            var board = ArenaDraftBoardComposer.Compose(
                7, Floor(), new List<ArenaDraftPartInfo>(), Loadout, 0);

            Assert.AreEqual(2, board.Count(e => e.PartId == "part.head.a"));
            Assert.AreEqual(board.Count, board.Select(e => e.EntryId).Distinct().Count());
        }

        [Test]
        public void Compose_CatalogSample_RespectsSampleSize()
        {
            var board = ArenaDraftBoardComposer.Compose(42, Floor(), Catalog(), Loadout, 2);

            Assert.AreEqual(Floor().Count + 2, board.Count);
        }

        [Test]
        public void Compose_SampleLargerThanCatalog_TakesTheWholeCatalog()
        {
            var board = ArenaDraftBoardComposer.Compose(42, Floor(), Catalog(), Loadout, 99);

            Assert.AreEqual(Floor().Count + Catalog().Count, board.Count);
        }

        [Test]
        public void Compose_CatalogPartAlreadyOnTheFloor_IsNotDuplicated()
        {
            var catalog = Catalog();
            catalog.Add(Part("part.head.a", "slot.head"));

            var board = ArenaDraftBoardComposer.Compose(42, Floor(), catalog, Loadout, 99);

            Assert.AreEqual(2, board.Count(e => e.PartId == "part.head.a"));
        }

        [Test]
        public void Compose_PartsOutsideTheSlotLoadout_AreDropped()
        {
            var floor = Floor();
            floor.Add(Part("part.tail.a", "slot.tail"));
            var catalog = Catalog();
            catalog.Add(Part("part.tail.x", "slot.tail"));

            var board = ArenaDraftBoardComposer.Compose(42, floor, catalog, Loadout, 99);

            Assert.IsFalse(board.Any(e => e.SlotId == "slot.tail"));
        }

        [Test]
        public void Compose_DifferentSeeds_CanSampleDifferentCatalogParts()
        {
            // With 4 candidates choose 1, some pair of seeds must differ; probe a few.
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 16; seed++)
            {
                var board = ArenaDraftBoardComposer.Compose(seed, Floor(), Catalog(), Loadout, 1);
                seen.Add(board.Last().PartId);
            }

            Assert.Greater(seen.Count, 1);
        }

        [Test]
        public void Compose_EntryIdsAreSequentialFromOne()
        {
            var board = ArenaDraftBoardComposer.Compose(42, Floor(), Catalog(), Loadout, 2);

            Assert.AreEqual(
                Enumerable.Range(1, board.Count).ToList(),
                board.Select(e => e.EntryId).ToList());
        }
    }
}
