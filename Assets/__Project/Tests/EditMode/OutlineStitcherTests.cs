using System.Collections.Generic;
using LevelGeneration.Surface;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class OutlineStitcherTests
    {
        private const float HexSize = 2f; // Max stitched notch depth = 1.5 at this size.

        // 10x10 CCW square with an optional notch of the given depth in the top edge.
        private static List<(float X, float Z)> SquareWithNotch(float notchDepth)
        {
            return new List<(float X, float Z)>
            {
                (0f, 0f), (10f, 0f), (10f, 10f),
                (6f, 10f), (5f, 10f - notchDepth), (4f, 10f),
                (0f, 10f)
            };
        }

        private static bool FillsContainVertex(
            List<((float X, float Z) A, (float X, float Z) B, (float X, float Z) C)> fills,
            (float X, float Z) vertex)
        {
            foreach (var (a, b, c) in fills)
            {
                if (a.Equals(vertex) || b.Equals(vertex) || c.Equals(vertex))
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void ConvexOutline_IsUnchanged_NoFills()
        {
            var square = new List<(float X, float Z)> { (0f, 0f), (10f, 0f), (10f, 10f), (0f, 10f) };

            var stitched = OutlineStitcher.Stitch(square, HexSize);

            CollectionAssert.AreEqual(square, stitched.Outline);
            Assert.IsEmpty(stitched.NotchFills);
        }

        [Test]
        public void ShallowNotch_IsSewnShut_AndPavedWithAFill()
        {
            var stitched = OutlineStitcher.Stitch(SquareWithNotch(notchDepth: 1f), HexSize);

            Assert.AreEqual(6, stitched.Outline.Count);
            CollectionAssert.DoesNotContain(stitched.Outline, (5f, 9f));

            // One triangle paves the notch: it spans the sewn vertex and the chord ends.
            Assert.AreEqual(1, stitched.NotchFills.Count);
            Assert.IsTrue(FillsContainVertex(stitched.NotchFills, (5f, 9f)));
            Assert.IsTrue(FillsContainVertex(stitched.NotchFills, (6f, 10f)));
            Assert.IsTrue(FillsContainVertex(stitched.NotchFills, (4f, 10f)));
        }

        [Test]
        public void NotchFills_AreWoundClockwiseInXz()
        {
            var stitched = OutlineStitcher.Stitch(SquareWithNotch(notchDepth: 1f), HexSize);

            foreach (var (a, b, c) in stitched.NotchFills)
            {
                float doubleArea = (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
                Assert.Less(doubleArea, 0f, "Fill triangles must be clockwise in XZ (Unity up-facing)");
            }
        }

        [Test]
        public void DeepBay_KeepsItsShape_NoFills()
        {
            var outline = SquareWithNotch(notchDepth: 4f);

            var stitched = OutlineStitcher.Stitch(outline, HexSize);

            CollectionAssert.AreEqual(outline, stitched.Outline);
            Assert.IsEmpty(stitched.NotchFills);
        }

        [Test]
        public void Stitching_IsWindingAgnostic()
        {
            var ccw = SquareWithNotch(notchDepth: 1f);
            var cw = new List<(float X, float Z)>(ccw);
            cw.Reverse();

            var stitchedCw = OutlineStitcher.Stitch(cw, HexSize);

            Assert.AreEqual(6, stitchedCw.Outline.Count);
            CollectionAssert.DoesNotContain(stitchedCw.Outline, (5f, 9f));
            Assert.AreEqual(1, stitchedCw.NotchFills.Count);
            Assert.IsTrue(FillsContainVertex(stitchedCw.NotchFills, (5f, 9f)));
        }

        [Test]
        public void TinyOrMissingOutline_PassesThrough()
        {
            var triangle = new List<(float X, float Z)> { (0f, 0f), (1f, 0f), (0f, 1f) };

            CollectionAssert.AreEqual(triangle, OutlineStitcher.Stitch(triangle, HexSize).Outline);
            Assert.IsEmpty(OutlineStitcher.Stitch(null, HexSize).Outline);
        }
    }
}
