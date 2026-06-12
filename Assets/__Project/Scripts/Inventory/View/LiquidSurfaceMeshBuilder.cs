using System.Collections.Generic;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// Builds the liquid surface as a tessellated pie sector matching the cauldron's
    /// swept arc, so its straight edges line up with the wall's cross-section cut
    /// planes. Tessellation rings give the boil animation vertices to displace.
    /// </summary>
    public static class LiquidSurfaceMeshBuilder
    {
        public static Mesh Build(float radius, float arcDegrees, int rings, int radialSegments)
        {
            rings = Mathf.Max(1, rings);
            radialSegments = Mathf.Max(3, radialSegments);

            float arcRadians = arcDegrees * Mathf.Deg2Rad;
            // Matches the cauldron sweep: centered on +Z, open edge toward -Z.
            float startAngle = -arcRadians * 0.5f;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            vertices.Add(Vector3.zero);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int ring = 1; ring <= rings; ring++)
            {
                float ringRadius = radius * ring / rings;
                for (int s = 0; s <= radialSegments; s++)
                {
                    float angle = startAngle + arcRadians * s / radialSegments;
                    var point = new Vector3(Mathf.Sin(angle) * ringRadius, 0f, Mathf.Cos(angle) * ringRadius);
                    vertices.Add(point);
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(point.x / (radius * 2f) + 0.5f, point.z / (radius * 2f) + 0.5f));
                }
            }

            // Innermost ring fans out from the center vertex.
            for (int s = 0; s < radialSegments; s++)
            {
                triangles.Add(0);
                triangles.Add(1 + s);
                triangles.Add(1 + s + 1);
            }

            // Remaining rings connect as quad grids (two triangles per cell).
            int verticesPerRing = radialSegments + 1;
            for (int ring = 1; ring < rings; ring++)
            {
                int innerStart = 1 + (ring - 1) * verticesPerRing;
                int outerStart = innerStart + verticesPerRing;
                for (int s = 0; s < radialSegments; s++)
                {
                    triangles.Add(innerStart + s);
                    triangles.Add(outerStart + s);
                    triangles.Add(outerStart + s + 1);

                    triangles.Add(innerStart + s);
                    triangles.Add(outerStart + s + 1);
                    triangles.Add(innerStart + s + 1);
                }
            }

            var mesh = new Mesh();
            mesh.name = "LiquidSurfaceMesh";
            mesh.MarkDynamic();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
