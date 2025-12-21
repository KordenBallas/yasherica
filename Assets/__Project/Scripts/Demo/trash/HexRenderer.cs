using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexRenderer : MonoBehaviour
{
    private Mesh hexMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    public Material hexMaterial;
    
    private List<Face> faces = new List<Face>();
    [SerializeField] public float innerSize = 0;
    [SerializeField] public float outerSize = 1;
    [SerializeField] public float height = 0.2f;
    [SerializeField] public bool isFlatTopped = false;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        hexMesh = new Mesh();
        hexMesh.name = "Hex Mesh";
        meshFilter.mesh = hexMesh;
        meshRenderer.material = hexMaterial;
    }

    private void OnEnable()
    {
        DrawMesh();
    }

    public void OnValidate()
    {
        if (Application.isPlaying)
        {
            DrawMesh();
        }
    }

    public void DrawMesh()
    {
        DrawFaces();
        CombineFaces();
    }

    private void CombineFaces()
    {
        List<Vector3> combinedVertices = new List<Vector3>();
        List<int> combinedTriangles = new List<int>();
        List<Vector2> combinedUVs = new List<Vector2>();

        int vertexOffset = 0;

        foreach (var face in faces)
        {
            combinedVertices.AddRange(face.vertices);
            combinedUVs.AddRange(face.uvs);

            foreach (var triangle in face.triangles)
            {
                combinedTriangles.Add(triangle + vertexOffset);
            }

            vertexOffset += face.vertices.Count;
        }

        hexMesh.Clear();
        hexMesh.SetVertices(combinedVertices);
        hexMesh.SetTriangles(combinedTriangles, 0);
        hexMesh.SetUVs(0, combinedUVs);
        hexMesh.RecalculateNormals();
    }

    private void DrawFaces()
    {
        faces.Clear();
        
        // Top faces
        for (int point = 0; point < 6; point++)
        {
            Face face = CreateHexFace(innerSize, outerSize, height / 2f, height / 2f, point, false);
            faces.Add(face);
        }
        // Bottom faces
        for (int point = 0; point < 6; point++)
        {
            Face face = CreateHexFace(innerSize, outerSize, -height / 2f, -height / 2f, point, true);
            faces.Add(face);
        }
        // Outer faces
        for (int point = 0; point < 6; point++)
        {
            Face face = CreateHexFace(outerSize, outerSize, height / 2f, -height / 2f, point, true);
            faces.Add(face);
        }
        // Inner faces
        for (int point = 0; point < 6; point++)
        {
            Face face = CreateHexFace(innerSize, innerSize, height / 2f, -height / 2f, point, false);
            faces.Add(face);
        }
    }

    private Face CreateHexFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverse = false)
    {
        Vector3 pointA = GetPoint(innerRad, heightB, point);
        Vector3 pointB = GetPoint(innerRad, heightB, (point<5)? point + 1 : 0);
        Vector3 pointC = GetPoint(outerRad, heightA, (point<5)? point + 1 : 0);
        Vector3 pointD = GetPoint(outerRad, heightA, point);
        
        List<Vector3> vertices = new List<Vector3>
        {
            pointA, pointB, pointC, pointD
        };
        List<int> triangles = new List<int>
        {
            0,1,2,
            2,3,0
        };
        List<Vector2> uvs = new List<Vector2>
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1)
        };
        if (reverse)
        {
            vertices.Reverse();
        }
        return new Face(vertices, triangles, uvs);        
    }
    
    private Vector3 GetPoint(float size, float height, int index)
    {
        float angleDeg = 60f * index;
        if (isFlatTopped) angleDeg -= 30f;
        float angleRad = Mathf.Deg2Rad * angleDeg;
        return new Vector3(size * Mathf.Cos(angleRad), height, size * Mathf.Sin(angleRad));
    }

    public struct Face
    {
        public List<Vector3> vertices { get; set; }
        public List<int> triangles { get; set; }
        public List<Vector2> uvs { get; set; }
        
        public Face(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.uvs = uvs;
        }
    }
    
}
