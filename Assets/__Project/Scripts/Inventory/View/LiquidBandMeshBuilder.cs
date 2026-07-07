using System.Collections.Generic;
using Inventory.Core;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// Builds a vertical band of liquid following the bowl's inner profile
    /// across an arc — the cut-away "curtain" that fills the cauldron's open
    /// wedge from floor to waterline (so submerged bubbles read through the
    /// liquid), and, as a thin band, the crisp waterline edge itself.
    /// Faces point outward (toward the cross-section camera).
    /// </summary>
    public static class LiquidBandMeshBuilder
    {
        public static Mesh Build(
            in CauldronProfileSettings bowl,
            float bottomHeight,
            float topHeight,
            float arcStartDegrees,
            float arcEndDegrees,
            int arcSegments,
            int heightSegments,
            float radialOffset,
            string meshName)
        {
            arcSegments = Mathf.Max(1, arcSegments);
            heightSegments = Mathf.Max(1, heightSegments);
            topHeight = Mathf.Max(topHeight, bottomHeight + 0.0001f);

            int columns = arcSegments + 1;
            var vertices = new List<Vector3>((heightSegments + 1) * columns);
            var normals = new List<Vector3>((heightSegments + 1) * columns);
            var uvs = new List<Vector2>((heightSegments + 1) * columns);
            var triangles = new List<int>(heightSegments * arcSegments * 6);

            float arcStartRadians = arcStartDegrees * Mathf.Deg2Rad;
            float arcEndRadians = arcEndDegrees * Mathf.Deg2Rad;

            for (int row = 0; row <= heightSegments; row++)
            {
                float tRow = row / (float)heightSegments;
                float height = Mathf.Lerp(bottomHeight, topHeight, tRow);
                float radius = Mathf.Max(
                    0.001f,
                    CauldronProfileCalculator.InnerRadiusAtHeight(bowl, height) + radialOffset);

                for (int s = 0; s < columns; s++)
                {
                    float angle = Mathf.Lerp(arcStartRadians, arcEndRadians, s / (float)arcSegments);
                    var outward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    vertices.Add(new Vector3(outward.x * radius, height, outward.z * radius));
                    normals.Add(outward);
                    uvs.Add(new Vector2(s / (float)arcSegments, tRow));
                }
            }

            for (int row = 0; row < heightSegments; row++)
            {
                int lower = row * columns;
                int upper = lower + columns;
                for (int s = 0; s < arcSegments; s++)
                {
                    // Wound clockwise as seen from outside the band, so the
                    // front faces point outward with the normals.
                    triangles.Add(lower + s);
                    triangles.Add(lower + s + 1);
                    triangles.Add(upper + s);

                    triangles.Add(upper + s);
                    triangles.Add(lower + s + 1);
                    triangles.Add(upper + s + 1);
                }
            }

            var mesh = new Mesh();
            mesh.name = meshName;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
