using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class AbilityShapeCalculatorTests
    {
        private HexDirectionConfig _config;
        private AbilityShapeCalculator _calculator;

        // Flat-top hex offsets matching production config
        private static readonly HexDirectionOffset[] FlatTopOffsets =
        {
            new HexDirectionOffset(HexDirection.E,  new Vector2Int(+1,  0)),
            new HexDirectionOffset(HexDirection.SE, new Vector2Int( 0, +1)),
            new HexDirectionOffset(HexDirection.SW, new Vector2Int(-1, +1)),
            new HexDirectionOffset(HexDirection.W,  new Vector2Int(-1,  0)),
            new HexDirectionOffset(HexDirection.NW, new Vector2Int( 0, -1)),
            new HexDirectionOffset(HexDirection.NE, new Vector2Int(+1, -1)),
        };

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<HexDirectionConfig>();
            _config.directionOffsets = FlatTopOffsets;
            _calculator = new AbilityShapeCalculator(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ── Line tests ──────────────────────────────────────────────────────

        [Test]
        public void Line_Length1_East_ReturnsSingleAdjacentCell()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForLine(1);

            var cells = _calculator.GetAffectedCells(shape, origin, HexDirection.E, _ => true);

            Assert.AreEqual(1, cells.Count);
            Assert.AreEqual(new HexCoordinates(1, 0), cells[0]);
        }

        [Test]
        public void Line_Length3_East_ReturnsThreeCellsInOrder()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForLine(3);

            var cells = _calculator.GetAffectedCells(shape, origin, HexDirection.E, _ => true);

            Assert.AreEqual(3, cells.Count);
            Assert.AreEqual(new HexCoordinates(1, 0), cells[0]);
            Assert.AreEqual(new HexCoordinates(2, 0), cells[1]);
            Assert.AreEqual(new HexCoordinates(3, 0), cells[2]);
        }

        [Test]
        public void Line_NoDirection_ReturnsEmpty()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForLine(3);

            var cells = _calculator.GetAffectedCells(shape, origin, null, _ => true);

            Assert.AreEqual(0, cells.Count);
        }

        [Test]
        public void Line_TruncatesAtBoundary()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForLine(5);

            // Only cells with Q <= 2 are in bounds
            var cells = _calculator.GetAffectedCells(
                shape, origin, HexDirection.E,
                c => c.Q <= 2);

            Assert.AreEqual(2, cells.Count);
            Assert.AreEqual(new HexCoordinates(1, 0), cells[0]);
            Assert.AreEqual(new HexCoordinates(2, 0), cells[1]);
        }

        [TestCase(HexDirection.E)]
        [TestCase(HexDirection.NE)]
        [TestCase(HexDirection.NW)]
        [TestCase(HexDirection.W)]
        [TestCase(HexDirection.SW)]
        [TestCase(HexDirection.SE)]
        public void Line_Length1_AllDirectionsReturnOneCell(HexDirection dir)
        {
            var shape = AbilityShapeData.ForLine(1);
            var cells = _calculator.GetAffectedCells(shape, new HexCoordinates(5, 5), dir, _ => true);
            Assert.AreEqual(1, cells.Count);
        }

        // ── Ring tests ───────────────────────────────────────────────────────

        [Test]
        public void Ring_Radius1_Returns6AdjacentCells()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForRing(1);

            var cells = _calculator.GetAffectedCells(shape, origin, null, _ => true);

            Assert.AreEqual(6, cells.Count);
        }

        [Test]
        public void Ring_Radius1_DoesNotContainCenter()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForRing(1);

            var cells = _calculator.GetAffectedCells(shape, origin, null, _ => true);

            Assert.IsFalse(cells.Contains(origin));
        }

        [Test]
        public void Ring_Radius2_Returns12Cells()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForRing(2);

            var cells = _calculator.GetAffectedCells(shape, origin, null, _ => true);

            Assert.AreEqual(12, cells.Count);
        }

        [Test]
        public void Ring_Radius2_ExcludesRadius1Cells()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForRing(2);

            var cells = _calculator.GetAffectedCells(shape, origin, null, _ => true);

            // No cell should be at cube-distance 1 from origin
            foreach (var c in cells)
            {
                int dist = HexDistance(origin, c);
                Assert.AreEqual(2, dist, $"Cell {c.Q},{c.R} is at distance {dist}, expected 2");
            }
        }

        [Test]
        public void Ring_FiltersBoundary()
        {
            var origin = new HexCoordinates(0, 0);
            var shape = AbilityShapeData.ForRing(1);

            // Only allow Q >= 0
            var cells = _calculator.GetAffectedCells(shape, origin, null, c => c.Q >= 0);

            Assert.IsTrue(cells.All(c => c.Q >= 0));
            Assert.Less(cells.Count, 6);
        }

        private static int HexDistance(HexCoordinates from, HexCoordinates to)
        {
            int dq = System.Math.Abs(from.Q - to.Q);
            int dr = System.Math.Abs(from.R - to.R);
            int ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));
            return (dq + dr + ds) / 2;
        }
    }
}
