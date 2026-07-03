using System.Collections.Generic;
using Combat.Battlefield;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Builds the platform mesh from its <see cref="PlatformHexSurface"/> — the hex-composed walkable
    /// top with flat fills paving the sewn boundary notches (the floor reaches the stitched outline
    /// everywhere), the drooping organic rim, the sides, and a mirrored bottom cap. The muted traversal
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
            BuildNotchFills(surface, verts, tris, y: 0f, facingUp: true);
            BuildRimStrip(surface, verts, tris, innerY: 0f, outerY: -rimDropHeight, facingOut: true);
            BuildVerticalStrip(surface.RimRing, verts, tris, topY: -rimDropHeight, bottomY: -thickness);
            BuildRimStrip(surface, verts, tris, innerY: -thickness, outerY: -thickness, facingOut: false);
            BuildNotchFills(surface, verts, tris, y: -thickness, facingUp: false);
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

        /// <summary>
        /// Flat floor patches paving the sewn boundary notches, so the walkable top (and its
        /// mirrored underside) reaches the stitched outline everywhere. Fills come from the surface
        /// wound clockwise in XZ (up-facing); the underside reverses them.
        /// </summary>
        private static void BuildNotchFills(PlatformHexSurface surface, List<Vector3> verts, List<int> tris,
            float y, bool facingUp)
        {
            foreach (var (a, b, c) in surface.NotchFills)
            {
                int start = verts.Count;
                verts.Add(new Vector3(a.X, y, a.Z));
                verts.Add(new Vector3(b.X, y, b.Z));
                verts.Add(new Vector3(c.X, y, c.Z));

                if (facingUp)
                {
                    tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                }
                else
                {
                    tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                }
            }
        }

        /// <summary>
        /// Quad strip between the subdivided walkable outline (at <paramref name="innerY"/>) and the
        /// jittered rim ring (at <paramref name="outerY"/>) — sloped when the heights differ (the
        /// drooping top rim), flat when they match (the mirrored underside ring).
        /// </summary>
        private static void BuildRimStrip(PlatformHexSurface surface, List<Vector3> verts, List<int> tris,
            float innerY, float outerY, bool facingOut)
        {
            var inner = surface.SubdividedOutline;
            var outer = surface.RimRing;
            for (int i = 0; i < inner.Count; i++)
            {
                int next = (i + 1) % inner.Count;
                var a = new Vector3(inner[i].X, innerY, inner[i].Z);
                var b = new Vector3(inner[next].X, innerY, inner[next].Z);
                var outerB = new Vector3(outer[next].X, outerY, outer[next].Z);
                var outerA = new Vector3(outer[i].X, outerY, outer[i].Z);
                AddQuad(verts, tris, a, b, outerB, outerA, facingOut);
            }
        }

        /// <summary>
        /// Outward-facing vertical strip along a ring (the side skirt). Skipped when the span is
        /// zero-height (the rim drop clamped to the full thickness leaves no skirt).
        /// </summary>
        private static void BuildVerticalStrip(IReadOnlyList<(float X, float Z)> ring,
            List<Vector3> verts, List<int> tris, float topY, float bottomY)
        {
            if (Mathf.Approximately(topY, bottomY))
            {
                return;
            }

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
