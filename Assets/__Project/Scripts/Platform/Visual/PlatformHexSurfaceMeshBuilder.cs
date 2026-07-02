using System.Collections.Generic;
using Combat.Battlefield;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Builds the platform mesh from its <see cref="PlatformHexSurface"/> — the hex-composed walkable
    /// top, the drooping organic rim, the sides, and a mirrored bottom cap. The muted traversal
    /// tiling (brief §2) is geometry, not shader: every cell top is a shallow dome (center raised by
    /// <c>cellInset</c>), so cell borders read as soft valleys under any lit material; 0 = flat.
    /// Faces do not share vertices, so the normals stay faceted and the tiling reads. Deterministic —
    /// all randomness (rim jitter) is already baked into the surface.
    /// </summary>
    public static class PlatformHexSurfaceMeshBuilder
    {
        public static Mesh Build(PlatformHexSurface surface, float thickness, float rimDropHeight, float cellInset)
        {
            // The rim can never droop below the platform underside.
            rimDropHeight = Mathf.Min(rimDropHeight, thickness);

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            BuildCellCaps(surface, verts, tris, topY: 0f, domeHeight: cellInset, facingUp: true);
            BuildRimStrip(surface, verts, tris, topY: 0f, bottomY: -rimDropHeight, facingOut: true, dropToRim: true);
            BuildSideSkirt(surface, verts, tris, topY: -rimDropHeight, bottomY: -thickness);
            BuildRimStrip(surface, verts, tris, topY: -thickness, bottomY: -thickness, facingOut: false, dropToRim: true);
            BuildCellCaps(surface, verts, tris, topY: -thickness, domeHeight: 0f, facingUp: false);

            FillPlanarUvs(verts, uvs);

            var mesh = new Mesh();
            mesh.name = "PlatformHexMesh";
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>One 7-vertex fan per cell; the top cap raises the fan center by the dome height.</summary>
        private static void BuildCellCaps(PlatformHexSurface surface, List<Vector3> verts, List<int> tris,
            float topY, float domeHeight, bool facingUp)
        {
            foreach (var cell in surface.Cells)
            {
                var (cx, cz) = surface.GetCellCenterLocal(cell);
                int centerIndex = verts.Count;
                verts.Add(new Vector3(cx, topY + domeHeight, cz));

                for (int i = 0; i < HexMetrics.CornerCount; i++)
                {
                    var (ox, oz) = HexMetrics.Corner(i, surface.Orientation, surface.HexSize);
                    verts.Add(new Vector3(cx + ox, topY, cz + oz));
                }

                for (int i = 0; i < HexMetrics.CornerCount; i++)
                {
                    int current = centerIndex + 1 + i;
                    int next = centerIndex + 1 + (i + 1) % HexMetrics.CornerCount;
                    if (facingUp)
                    {
                        // Corners are counter-clockwise in XZ; Unity's up-facing winding reverses them.
                        tris.Add(centerIndex);
                        tris.Add(next);
                        tris.Add(current);
                    }
                    else
                    {
                        tris.Add(centerIndex);
                        tris.Add(current);
                        tris.Add(next);
                    }
                }
            }
        }

        /// <summary>Quad strip between the subdivided walkable outline and the jittered rim ring.</summary>
        private static void BuildRimStrip(PlatformHexSurface surface, List<Vector3> verts, List<int> tris,
            float topY, float bottomY, bool facingOut, bool dropToRim)
        {
            var inner = surface.SubdividedOutline;
            var outer = surface.RimRing;
            for (int i = 0; i < inner.Count; i++)
            {
                int next = (i + 1) % inner.Count;
                var a = new Vector3(inner[i].X, topY, inner[i].Z);
                var b = new Vector3(inner[next].X, topY, inner[next].Z);
                var outerB = new Vector3(outer[next].X, dropToRim ? bottomY : topY, outer[next].Z);
                var outerA = new Vector3(outer[i].X, dropToRim ? bottomY : topY, outer[i].Z);
                AddQuad(verts, tris, a, b, outerB, outerA, facingOut);
            }
        }

        /// <summary>Vertical skirt from the rim ring down to the platform underside.</summary>
        private static void BuildSideSkirt(PlatformHexSurface surface, List<Vector3> verts, List<int> tris,
            float topY, float bottomY)
        {
            var ring = surface.RimRing;
            for (int i = 0; i < ring.Count; i++)
            {
                int next = (i + 1) % ring.Count;
                var a = new Vector3(ring[i].X, topY, ring[i].Z);
                var b = new Vector3(ring[next].X, topY, ring[next].Z);
                var botB = new Vector3(ring[next].X, bottomY, ring[next].Z);
                var botA = new Vector3(ring[i].X, bottomY, ring[i].Z);
                AddQuad(verts, tris, a, b, botB, botA, facingOut: true);
            }
        }

        private static void AddQuad(List<Vector3> verts, List<int> tris,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool facingOut)
        {
            int start = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);

            if (facingOut)
            {
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
            }
            else
            {
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
            }
        }

        private static void FillPlanarUvs(List<Vector3> verts, List<Vector2> uvs)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var v in verts)
            {
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
                minZ = Mathf.Min(minZ, v.z);
                maxZ = Mathf.Max(maxZ, v.z);
            }

            float width = Mathf.Max(maxX - minX, 1e-4f);
            float depth = Mathf.Max(maxZ - minZ, 1e-4f);
            foreach (var v in verts)
            {
                uvs.Add(new Vector2((v.x - minX) / width, (v.z - minZ) / depth));
            }
        }
    }
}
