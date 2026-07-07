using System.Collections.Generic;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// Procedural flat meshes for the blank medallion (Track F): the backing
    /// disc and the rim-progress arc segments. Built in the XY plane facing the
    /// stage camera (-Z), so they slot straight into the entry hierarchy.
    /// </summary>
    public static class MedallionMeshBuilder
    {
        public static Mesh BuildDisc(float radius, int segments)
        {
            segments = Mathf.Max(3, segments);

            var vertices = new List<Vector3>(segments + 1) { Vector3.zero };
            var triangles = new List<int>(segments * 3);

            for (int i = 0; i < segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            for (int i = 0; i < segments; i++)
            {
                int current = 1 + i;
                int next = 1 + (i + 1) % segments;
                // Clockwise as seen from -Z (the camera side), so the face
                // points toward the viewer like the built-in quad.
                triangles.Add(0);
                triangles.Add(next);
                triangles.Add(current);
            }

            return Assemble(vertices, triangles, "MedallionDiscMesh");
        }

        public static Mesh BuildRimArc(
            float innerRadius,
            float outerRadius,
            float startDegrees,
            float endDegrees,
            int segments)
        {
            segments = Mathf.Max(1, segments);

            int columns = segments + 1;
            var vertices = new List<Vector3>(columns * 2);
            var triangles = new List<int>(segments * 6);

            for (int i = 0; i < columns; i++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, i / (float)segments) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices.Add(direction * innerRadius);
                vertices.Add(direction * outerRadius);
            }

            for (int i = 0; i < segments; i++)
            {
                int inner0 = i * 2;
                int outer0 = inner0 + 1;
                int inner1 = inner0 + 2;
                int outer1 = inner0 + 3;

                triangles.Add(inner0);
                triangles.Add(inner1);
                triangles.Add(outer0);

                triangles.Add(outer0);
                triangles.Add(inner1);
                triangles.Add(outer1);
            }

            return Assemble(vertices, triangles, "MedallionRimArcMesh");
        }

        private static Mesh Assemble(List<Vector3> vertices, List<int> triangles, string meshName)
        {
            var normals = new List<Vector3>(vertices.Count);
            var uvs = new List<Vector2>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                normals.Add(Vector3.back);
                uvs.Add(new Vector2(vertices[i].x * 0.5f + 0.5f, vertices[i].y * 0.5f + 0.5f));
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
