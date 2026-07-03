using System.Collections.Generic;
using Combat.Battlefield;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;
using Platform;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Locks the drooping-rim edge profile: the walkable outline stays on the top plane, the rim
    /// strip slopes down to the jittered rim ring at -rimDrop, and the skirt closes the mesh down
    /// to the underside.
    /// </summary>
    [TestFixture]
    public class PlatformHexSurfaceMeshBuilderTests
    {
        private const float Thickness = 1f;
        private const float RimDrop = 0.4f;
        private const float CellInset = 0.06f;
        private const float Epsilon = 1e-3f;

        private static PlatformHexSurface GenerateSurface(ulong seed = 7)
        {
            return new PlatformSurfaceGenerator().Generate(
                new ShapeProfile(12, 18, 6), guaranteedMinCells: 12, hexSize: 2f,
                orientation: HexOrientation.Flat, rimWidth: 1.2f, rimJitterPercent: 35,
                rng: new DeterministicRandom(seed));
        }

        private static List<float> YsAtColumn(Vector3[] vertices, float x, float z)
        {
            var ys = new List<float>();
            foreach (var v in vertices)
            {
                if (Mathf.Abs(v.x - x) < Epsilon && Mathf.Abs(v.z - z) < Epsilon)
                {
                    ys.Add(v.y);
                }
            }

            return ys;
        }

        private static bool ContainsY(List<float> ys, float y)
        {
            foreach (var candidate in ys)
            {
                if (Mathf.Abs(candidate - y) < Epsilon)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void WalkableOutline_StaysOnTopPlane_AndUnderside()
        {
            var surface = GenerateSurface();
            var vertices = PlatformHexSurfaceMeshBuilder.Build(surface, Thickness, RimDrop, CellInset).vertices;

            foreach (var (x, z) in surface.SubdividedOutline)
            {
                var ys = YsAtColumn(vertices, x, z);
                Assert.IsTrue(ContainsY(ys, 0f), $"No top vertex at outline point ({x}, {z})");
                Assert.IsTrue(ContainsY(ys, -Thickness), $"No underside vertex at outline point ({x}, {z})");
                foreach (var y in ys)
                {
                    Assert.IsTrue(
                        Mathf.Abs(y) < Epsilon || Mathf.Abs(y + Thickness) < Epsilon,
                        $"Outline point ({x}, {z}) has an unexpected vertex at y={y}");
                }
            }
        }

        [Test]
        public void RimRing_DroopsToRimDrop_AndCarriesTheSkirt()
        {
            var surface = GenerateSurface();
            var vertices = PlatformHexSurfaceMeshBuilder.Build(surface, Thickness, RimDrop, CellInset).vertices;

            foreach (var (x, z) in surface.RimRing)
            {
                var ys = YsAtColumn(vertices, x, z);
                Assert.IsTrue(ContainsY(ys, -RimDrop), $"No drooped rim vertex at rim point ({x}, {z})");
                Assert.IsTrue(ContainsY(ys, -Thickness), $"No underside vertex at rim point ({x}, {z})");
                foreach (var y in ys)
                {
                    Assert.IsTrue(
                        Mathf.Abs(y + RimDrop) < Epsilon || Mathf.Abs(y + Thickness) < Epsilon,
                        $"Rim point ({x}, {z}) has an unexpected vertex at y={y}");
                }
            }
        }

        [Test]
        public void NotchFills_PaveTopAndUnderside()
        {
            var surface = GenerateSurface();
            var vertices = PlatformHexSurfaceMeshBuilder.Build(surface, Thickness, RimDrop, CellInset).vertices;

            foreach (var (a, b, c) in surface.NotchFills)
            {
                foreach (var p in new[] { a, b, c })
                {
                    var ys = YsAtColumn(vertices, p.X, p.Z);
                    Assert.IsTrue(ContainsY(ys, 0f), $"No top fill vertex at ({p.X}, {p.Z})");
                    Assert.IsTrue(ContainsY(ys, -Thickness), $"No underside fill vertex at ({p.X}, {p.Z})");
                }
            }
        }

        [Test]
        public void Mesh_BottomsOutAtTheUnderside_NoNaNs()
        {
            var surface = GenerateSurface();
            var vertices = PlatformHexSurfaceMeshBuilder.Build(surface, Thickness, RimDrop, CellInset).vertices;

            float minY = float.MaxValue;
            foreach (var v in vertices)
            {
                Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z), "NaN vertex");
                minY = Mathf.Min(minY, v.y);
            }

            Assert.AreEqual(-Thickness, minY, Epsilon);
        }

        [Test]
        public void FullDrop_ClampsCleanly_SkirtCollapsesWithoutDegenerateQuads()
        {
            var surface = GenerateSurface();
            var withSkirt = PlatformHexSurfaceMeshBuilder.Build(surface, Thickness, RimDrop, CellInset).vertices;
            var fullDrop = PlatformHexSurfaceMeshBuilder.Build(surface, Thickness, Thickness, CellInset).vertices;

            // The zero-height skirt strip is skipped entirely: 4 vertices per rim-ring segment.
            Assert.AreEqual(withSkirt.Length - surface.RimRing.Count * 4, fullDrop.Length);

            foreach (var (x, z) in surface.RimRing)
            {
                foreach (var y in YsAtColumn(fullDrop, x, z))
                {
                    Assert.AreEqual(-Thickness, y, Epsilon, $"Rim point ({x}, {z}) off the underside at full drop");
                }
            }
        }
    }
}
