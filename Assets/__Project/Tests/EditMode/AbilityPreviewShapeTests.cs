using System.Linq;
using NUnit.Framework;
using UI.AbilityPreview;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// The preview stage's mock-ground geometry: a Line yields exactly its length in cells
    /// marching away from the caster; a Ring yields the 6R hex-ring; both are deterministic.
    /// </summary>
    [TestFixture]
    public class AbilityPreviewShapeTests
    {
        private const float CellSize = 0.5f;

        [Test]
        public void Line_YieldsLengthCells_MarchingAwayFromOrigin()
        {
            var cells = AbilityPreviewShape.CellPositions(true, 3, 0, CellSize);

            Assert.AreEqual(3, cells.Count);
            for (int i = 1; i < cells.Count; i++)
            {
                Assert.Greater(cells[i].magnitude, cells[i - 1].magnitude);
            }
        }

        [TestCase(1, 6)]
        [TestCase(2, 12)]
        [TestCase(3, 18)]
        public void Ring_YieldsSixRadiusCells(int radius, int expectedCount)
        {
            var cells = AbilityPreviewShape.CellPositions(false, 0, radius, CellSize);

            Assert.AreEqual(expectedCount, cells.Count);
        }

        [Test]
        public void Ring_CellsAreDistinct_AndEquidistantEnoughFromOrigin()
        {
            var cells = AbilityPreviewShape.CellPositions(false, 0, 2, CellSize);

            Assert.AreEqual(cells.Count, cells.Distinct().Count(), "no duplicate cells");
            // Every ring cell sits between R*inner and R*outer hex metrics from the caster.
            float min = cells.Min(c => c.magnitude);
            float max = cells.Max(c => c.magnitude);
            Assert.Greater(min, 0f);
            Assert.LessOrEqual(max / min, 1.2f, "a hex ring is nearly circular");
        }

        [Test]
        public void ZeroRadiusRing_YieldsNoCells()
        {
            Assert.IsEmpty(AbilityPreviewShape.CellPositions(false, 0, 0, CellSize));
        }

        [Test]
        public void SameInputs_YieldIdenticalPositions()
        {
            var first = AbilityPreviewShape.CellPositions(false, 0, 2, CellSize);
            var second = AbilityPreviewShape.CellPositions(false, 0, 2, CellSize);

            CollectionAssert.AreEqual(first, second);
        }
    }
}
