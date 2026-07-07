using System;
using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Covers the Track F spot assignment: bottom-up pile occupancy and the
    /// column-scoped gravity settle (FR4, owner revision 2026-07-07): removing
    /// a bubble drops only the column that was resting above it; everything
    /// beside and below keeps its spot. Plus sync reconciliation and the
    /// fullness top readout.
    /// </summary>
    [TestFixture]
    public class BrewLayoutModelTests
    {
        // A 3-column lattice (columns -1/0/+1), two full rows, plus a third
        // row that exists only in the centre column — builder order:
        // rows bottom-up, centre-out within a row.
        private static readonly BrewSpot[] Spots =
        {
            new BrewSpot(0, 0, 0, 0f, 0.1f, 0f),
            new BrewSpot(1, 0, -1, -0.2f, 0.1f, 0.01f),
            new BrewSpot(2, 0, 1, 0.2f, 0.1f, -0.01f),
            new BrewSpot(3, 1, 0, 0f, 0.3f, 0.02f),
            new BrewSpot(4, 1, -1, -0.2f, 0.3f, 0f),
            new BrewSpot(5, 1, 1, 0.2f, 0.3f, 0.01f),
            new BrewSpot(6, 2, 0, 0f, 0.5f, -0.02f)
        };

        private BrewLayoutModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new BrewLayoutModel(Spots);
        }

        private void Occupy(params int[] instanceIds)
        {
            foreach (int id in instanceIds)
            {
                Assert.IsTrue(_model.TryOccupy(id, out _));
            }
        }

        private BrewSpot SpotOf(int instanceId)
        {
            Assert.IsTrue(_model.TryGetSpot(instanceId, out var spot));
            return spot;
        }

        [Test]
        public void Constructor_EmptyLattice_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BrewLayoutModel(null));
            Assert.Throws<ArgumentException>(() => new BrewLayoutModel(new BrewSpot[0]));
        }

        [Test]
        public void TryOccupy_AssignsLowestFreeSpotBottomUp()
        {
            Occupy(10, 11);

            Assert.AreEqual(0, SpotOf(10).Index);
            Assert.AreEqual(1, SpotOf(11).Index);
        }

        [Test]
        public void TryOccupy_IsIdempotentForAPlacedArtifact()
        {
            Occupy(10);
            var first = SpotOf(10);

            Assert.IsTrue(_model.TryOccupy(10, out var again));
            Assert.AreEqual(first.Index, again.Index);
        }

        [Test]
        public void Release_MidPile_OnlyTheColumnAboveFalls()
        {
            // 13 stacks on top of 10 in the centre column; 11/12 sit beside.
            Occupy(10, 11, 12, 13);
            Assert.AreEqual(3, SpotOf(13).Index);

            _model.Release(10);

            // The column above the removed bubble fell one place...
            var settled = SpotOf(13);
            Assert.AreEqual(0, settled.Index);
            Assert.AreEqual(0.1f, settled.Y, 1e-6f);
            // ...and the bubbles BESIDE it kept their spots (FR4: no re-sort).
            Assert.AreEqual(1, SpotOf(11).Index);
            Assert.AreEqual(2, SpotOf(12).Index);
        }

        [Test]
        public void Release_WithNothingAboveInItsColumn_MovesNobody()
        {
            Occupy(10, 11, 12);

            _model.Release(11);

            Assert.AreEqual(0, SpotOf(10).Index);
            Assert.AreEqual(2, SpotOf(12).Index);
            Assert.IsFalse(_model.TryGetSpot(11, out _));
        }

        [Test]
        public void Release_BottomOfATallColumn_DropsTheWholeColumnByOne_OthersStay()
        {
            // Fill everything: centre column three deep (spots 0/3/6).
            Occupy(10, 11, 12, 13, 14, 15, 16);

            _model.Release(10);

            // The centre column settles: 13 (row1) -> row0, 16 (row2) -> row1.
            Assert.AreEqual(0, SpotOf(13).Index);
            Assert.AreEqual(3, SpotOf(16).Index);
            // The side columns are untouched.
            Assert.AreEqual(1, SpotOf(11).Index);
            Assert.AreEqual(2, SpotOf(12).Index);
            Assert.AreEqual(4, SpotOf(14).Index);
            Assert.AreEqual(5, SpotOf(15).Index);
        }

        [Test]
        public void TryOccupy_ExhaustedLattice_ReturnsFalse()
        {
            for (int i = 0; i < Spots.Length; i++)
            {
                Assert.IsTrue(_model.TryOccupy(100 + i, out _));
            }

            Assert.IsFalse(_model.TryOccupy(999, out _));
        }

        [Test]
        public void Sync_ReleasesStale_SettlesTheirColumns_AndStacksNewOnTop()
        {
            _model.Sync(new List<int> { 10, 11, 12, 13 });

            _model.Sync(new List<int> { 11, 12, 13, 20 });

            Assert.IsFalse(_model.TryGetSpot(10, out _));
            // 13 fell down the centre column into the freed floor spot...
            Assert.AreEqual(0, SpotOf(13).Index);
            // ...the side columns stayed...
            Assert.AreEqual(1, SpotOf(11).Index);
            Assert.AreEqual(2, SpotOf(12).Index);
            // ...and the new artifact took the lowest free spot of the pile.
            Assert.AreEqual(3, SpotOf(20).Index);
        }

        [Test]
        public void HighestOccupiedY_FollowsTheColumnSettle()
        {
            Assert.AreEqual(0f, _model.HighestOccupiedY);

            Occupy(10, 11, 12, 13);
            Assert.AreEqual(0.3f, _model.HighestOccupiedY, 1e-6f);

            // The bottom of the tall column leaves; its top falls -> the pile is flat again.
            _model.Release(10);
            Assert.AreEqual(0.1f, _model.HighestOccupiedY, 1e-6f);
        }
    }
}
