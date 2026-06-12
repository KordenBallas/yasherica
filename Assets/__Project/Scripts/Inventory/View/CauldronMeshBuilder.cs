using System.Collections.Generic;
using Inventory.Core;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// Lathe-sweeps a cauldron profile into a flat-shaded low-poly mesh. The sweep
    /// is centered on +Z and covers less than a full turn, so the missing wedge is
    /// the front cross-section opening through which the interior is visible.
    /// Both cut planes are capped with quad strips between the paired profile
    /// points so the wall reads as solid.
    /// </summary>
    public static class CauldronMeshBuilder
    {
        // Quads collapsed below this area come from intentionally coincident
        // profile points (e.g. the inner rim) and are skipped.
        private const float DegenerateQuadSqrAreaEpsilon = 1e-12f;

        public static Mesh Build(CauldronProfile profile, int radialSegments, float arcDegrees)
        {
            radialSegments = Mathf.Max(3, radialSegments);
            float arcRadians = arcDegrees * Mathf.Deg2Rad;
            // Centering the arc on +Z points the opening at -Z, toward the stage camera.
            float startAngle = -arcRadians * 0.5f;
            float endAngle = startAngle + arcRadians;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            IReadOnlyList<ProfilePoint> outer = profile.Outer;
            IReadOnlyList<ProfilePoint> inner = profile.Inner;
            int last = outer.Count - 1;

            for (int j = 0; j < radialSegments; j++)
            {
                float angle0 = startAngle + arcRadians * j / radialSegments;
                float angle1 = startAngle + arcRadians * (j + 1) / radialSegments;
                float angleMid = (angle0 + angle1) * 0.5f;
                var radialDirection = new Vector3(Mathf.Sin(angleMid), 0f, Mathf.Cos(angleMid));

                for (int i = 0; i < last; i++)
                {
                    AddQuad(
                        LathePoint(outer[i], angle0),
                        LathePoint(outer[i], angle1),
                        LathePoint(outer[i + 1], angle1),
                        LathePoint(outer[i + 1], angle0),
                        SurfaceHint(outer[i], outer[i + 1], radialDirection, outward: true),
                        vertices, normals, uvs, triangles);

                    AddQuad(
                        LathePoint(inner[i], angle0),
                        LathePoint(inner[i], angle1),
                        LathePoint(inner[i + 1], angle1),
                        LathePoint(inner[i + 1], angle0),
                        SurfaceHint(inner[i], inner[i + 1], radialDirection, outward: false),
                        vertices, normals, uvs, triangles);
                }

                // Rim: flat strip between the outer lip and the inner mouth.
                AddQuad(
                    LathePoint(outer[last], angle0),
                    LathePoint(outer[last], angle1),
                    LathePoint(inner[last], angle1),
                    LathePoint(inner[last], angle0),
                    Vector3.up,
                    vertices, normals, uvs, triangles);
            }

            // Cut-plane caps: both face away from the solid wedge, into the opening.
            Vector3 startCapNormal = -TangentDirection(startAngle);
            Vector3 endCapNormal = TangentDirection(endAngle);
            for (int i = 0; i < last; i++)
            {
                AddQuad(
                    LathePoint(outer[i], startAngle),
                    LathePoint(outer[i + 1], startAngle),
                    LathePoint(inner[i + 1], startAngle),
                    LathePoint(inner[i], startAngle),
                    startCapNormal,
                    vertices, normals, uvs, triangles);

                AddQuad(
                    LathePoint(outer[i], endAngle),
                    LathePoint(outer[i + 1], endAngle),
                    LathePoint(inner[i + 1], endAngle),
                    LathePoint(inner[i], endAngle),
                    endCapNormal,
                    vertices, normals, uvs, triangles);
            }

            var mesh = new Mesh();
            mesh.name = "CauldronMesh";
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 LathePoint(ProfilePoint point, float angle)
        {
            return new Vector3(Mathf.Sin(angle) * point.Radius, point.Height, Mathf.Cos(angle) * point.Radius);
        }

        private static Vector3 TangentDirection(float angle)
        {
            return new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
        }

        // Approximate facing direction for a wall quad, derived from the profile
        // segment's 2D normal: (dh, -dr) points away from the solid on the outer
        // surface, the negation points into the cavity on the inner surface.
        private static Vector3 SurfaceHint(ProfilePoint from, ProfilePoint to, Vector3 radialDirection, bool outward)
        {
            float deltaRadius = to.Radius - from.Radius;
            float deltaHeight = to.Height - from.Height;
            float radialComponent = outward ? deltaHeight : -deltaHeight;
            float verticalComponent = outward ? -deltaRadius : deltaRadius;
            return (radialDirection * radialComponent + Vector3.up * verticalComponent).normalized;
        }

        // Adds a flat-shaded quad (a, b, c, d in cyclic order) whose front face is
        // flipped, if needed, to agree with the supplied facing hint.
        private static void AddQuad(
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normalHint,
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
        {
            Vector3 cross = Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a);
            if (cross.sqrMagnitude < DegenerateQuadSqrAreaEpsilon)
            {
                return;
            }

            Vector3 normal = cross.normalized;
            bool flip = Vector3.Dot(normal, normalHint) < 0f;
            if (flip)
            {
                normal = -normal;
            }

            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);

            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
            }

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            if (flip)
            {
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
            else
            {
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }
        }
    }
}
