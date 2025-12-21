using System.Collections.Generic;
using UnityEngine;

namespace Platform
{
    public static class PlatformMeshBuilder
    {
        public static Mesh BuildPlatformMesh(
            float length,
            float width,
            int vertexCount,
            float jitter,
            float thickness,
            out List<Vector3> outline)
        {
            vertexCount = Mathf.Max(6, vertexCount);
            List<Vector3> topVerts = new List<Vector3>();
            List<Vector2> topUV = new List<Vector2>();

            float a = length * 0.5f; // x-radius
            float b = width * 0.5f;  // z-radius

            for (int i = 0; i < vertexCount; i++)
            {
                float ang = (i / (float)vertexCount) * Mathf.PI * 2f;
                // base ellipse point
                float x = Mathf.Cos(ang) * a;
                float z = Mathf.Sin(ang) * b;
                // jitter radial by up to jitter * min(a,b)
                float r = 1f + (Random.Range(-jitter, jitter));
                x *= r; z *= r;
                topVerts.Add(new Vector3(x, 0f, z));
                topUV.Add(new Vector2((x / length) + 0.5f, (z / width) + 0.5f));
            }
            outline = new List<Vector3>(topVerts);

            // center vertex for top
            Vector3 center = Vector3.zero;

            // Build mesh arrays
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            // Top: fan triangles
            int topStart = verts.Count;
            verts.Add(center); normals.Add(Vector3.up); uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < topVerts.Count; i++)
            {
                verts.Add(topVerts[i]);
                normals.Add(Vector3.up);
                uvs.Add(topUV[i]);
            }

            // Top triangulation (counter-clockwise)
            for (int i = 0; i < topVerts.Count; i++)
            {
                int aIdx = topStart + 1 + i;
                int bIdx = topStart + 1 + ((i + 1) % topVerts.Count);
                tris.Add(topStart);
                tris.Add(bIdx);
                tris.Add(aIdx);
            }

            // Bottom verts (flat bottom at -thickness)
            int bottomStart = verts.Count;
            for (int i = 0; i < topVerts.Count; i++)
            {
                verts.Add(new Vector3(topVerts[i].x, -thickness, topVerts[i].z));
                normals.Add(Vector3.down);
                uvs.Add(topUV[i]);
            }

            // Bottom triangulation (reverse winding)
            for (int i = 0; i < topVerts.Count - 2; i++)
            {
                tris.Add(bottomStart + 0);
                tris.Add(bottomStart + i + 1);
                tris.Add(bottomStart + i + 2);
            }

            // Sides: quads per edge (as two triangles)
            for (int i = 0; i < topVerts.Count; i++)
            {
                int ni = (i + 1) % topVerts.Count;
                Vector3 vTopA = topVerts[i];
                Vector3 vTopB = topVerts[ni];
                Vector3 vBotA = new Vector3(vTopA.x, -thickness, vTopA.z);
                Vector3 vBotB = new Vector3(vTopB.x, -thickness, vTopB.z);
                int s0 = verts.Count;
                verts.Add(vTopA);
                verts.Add(vTopB);
                verts.Add(vBotB);
                verts.Add(vBotA);
                
                // normal for this face
                Vector3 faceNormal = Vector3.Cross(vTopB - vTopA, vBotA - vTopA).normalized;
                normals.Add(faceNormal);
                normals.Add(faceNormal);
                normals.Add(faceNormal);
                normals.Add(faceNormal);
                
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                
                // two triangles
                tris.Add(s0 + 0);
                tris.Add(s0 + 1);
                tris.Add(s0 + 2);
                tris.Add(s0 + 0);
                tris.Add(s0 + 2);
                tris.Add(s0 + 3);
            }

            Mesh mesh = new Mesh();
            mesh.name = "PlatformMesh";
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

