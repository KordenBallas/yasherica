// ProceduralPlatformGraphGenerator.cs
// 
// Usage:
// 1) Put this file into Assets/ (or Assets/Scripts/).
// 2) Create an empty GameObject in the scene and add the ProceduralPlatformGraphGenerator component.
// 3) Configure parameters in the Inspector: Platform Count, Gap Between Platforms, Height Deviation, Platform Size ranges, etc.
// 4) Press the "Generate" button in the inspector (or use the context menu GenerateGraph()).
// 5) The script will create a parent GameObject named "GeneratedPlatformGraph" containing platform meshes and a small Gizmo visualizer.
//
// What it does:
// - Builds a simple route graph where each node is a platform. The default graph is a linear route (path) visiting each platform in order.
// - Each platform gets a 2D footprint polygon (low-poly, imperfect / jaged natural edges) based on width/length and vertex jitter.
// - The polygon is extruded to produce a low-poly 3D mesh with a flat top and vertical-ish sides.
// - Platforms are placed along the X axis spaced by gap + half sizes, with random Y (height) offset chosen per node (up or down) by the HeightDeviation parameter.
// - The generator ensures platforms do not overlap by spacing them using platform extents + gap.
// - The component exposes a simple runtime API (GenerateGraph(), ClearGraph()).
//
// Notes / limitations:
// - This generates a simple linear graph (chain). If you want branching graphs, I can add options to create branching edges and more complex routing.
// - The polygon generator is intentionally low-poly: specify EdgeVertexCount (6..20) to control shape detail.
// - Mesh normals are computed by Unity and flattened somewhat by using duplicated vertices for top and sides for a low-poly look.
// - Materials: the generator creates a simple material per platform using a built-in shader (Standard). You can change materials later.

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ProceduralPlatformGraphGenerator : MonoBehaviour
{
    [Header("Graph Parameters")]
    [Tooltip("Number of platforms (nodes) in the route graph")]
    public int platformCount = 6;
    [Tooltip("Gap between neighboring platforms (world units)")]
    public float gapBetweenPlatforms = 2.0f;
    [Tooltip("Maximum absolute height deviation between consecutive platforms")]
    public float heightDeviation = 1.5f;
    [Tooltip("Deterministic seed. 0 uses random seed.")]
    public int seed = 0;

    [Header("Platform Shape")]
    [Tooltip("Min/Max size (length X, width Z) for platforms")]
    public Vector2 platformSizeMin = new Vector2(3f, 2f);
    public Vector2 platformSizeMax = new Vector2(6f, 4f);
    [Tooltip("Number of vertices around platform top edge (6..24). More -> more detailed jagged edge")]
    [Range(6, 24)] public int edgeVertexCount = 10;
    [Tooltip("Amount of jitter applied to the top edge in world units (relative to scale)")]
    [Range(0f, 0.8f)] public float edgeJitter = 0.25f;
    [Tooltip("Thickness of platform (height downwards from top) in world units")]
    public float platformThickness = 1.0f;

    [Header("Appearance")]
    public Material platformMaterial; // optional - if null a default will be created
    [Tooltip("Whether to color platforms with varying tint")]
    public bool colorVariation = true;

    [Header("Runtime / Debug")]
    public bool visualizeGizmos = true;

    // Internal graph representation
    public class Node { public int id; public Vector3 position; public Vector2 size; public GameObject go; public List<Vector3> topBoundary; }
    public class Edge { public int a, b; }
    public List<Node> nodes = new List<Node>();
    public List<Edge> edges = new List<Edge>();

    const string parentName = "GeneratedPlatformGraph";
    GameObject parentGO;

    void OnEnable()
    {
        GenerateGraph();
    }
    
    // --------------------------- Public API ---------------------------
    [ContextMenu("Generate Graph & Platforms")]
    public void GenerateGraph()
    {
        ClearGraph();
        if (seed != 0) Random.InitState(seed); else Random.InitState((int)System.DateTime.Now.Ticks & 0x0000FFFF);

        // clamp count
        platformCount = Mathf.Max(1, platformCount);

        // create parent
        parentGO = new GameObject(parentName);
        parentGO.transform.parent = this.transform;
        parentGO.transform.localPosition = Vector3.zero;

        // Build linear graph: nodes placed sequentially on X, with gaps
        nodes = new List<Node>();
        edges = new List<Edge>();

        float cursorX = 0f;
        // choose initial baseline Y
        float baselineY = 0f;
        for (int i = 0; i < platformCount; i++)
        {
            // random size in range
            float sx = Random.Range(platformSizeMin.x, platformSizeMax.x);
            float sz = Random.Range(platformSizeMin.y, platformSizeMax.y);

            // compute position: center at cursor + half width
            float posX = cursorX + sx * 0.5f;
            // height deviation relative to previous node
            float dy = 0f;
            if (i > 0)
            {
                // pick random up/down with magnitude up to heightDeviation
                dy = Random.Range(-heightDeviation, heightDeviation);
            }
            float posY = (i == 0) ? baselineY : (nodes[i - 1].position.y + dy);
            Vector3 pos = new Vector3(posX, posY, 0f);

            Node n = new Node() { id = i, position = pos, size = new Vector2(sx, sz), go = null, topBoundary = null };
            nodes.Add(n);

            // advance cursor: add size + gap
            cursorX += sx + gapBetweenPlatforms;

            // create edge to previous node
            if (i > 0) edges.Add(new Edge() { a = i - 1, b = i });
        }

        // create materials if missing
        if (platformMaterial == null)
        {
            Shader s = Shader.Find("Standard");
            platformMaterial = new Material(s);
            platformMaterial.name = "_GeneratedPlatformMat";
        }

        // Create platform GameObjects with mesh
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            List<Vector3> outline;
            GameObject go = CreatePlatformMeshGO(n.id, n.size.x, n.size.y, edgeVertexCount, edgeJitter, platformThickness, out outline);
            go.transform.parent = parentGO.transform;
            go.transform.localPosition = n.position;
            
            // Create Platform Colliders
            PlatformColliderBuilder.BuildPlatformColliders(go, outline);
            n.go = go;
            n.topBoundary = outline;
            nodes[i] = n;
            
            // apply material and color
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = platformMaterial;
            if (colorVariation)
            {
                Color c = Color.HSVToRGB((i / (float)Mathf.Max(1, nodes.Count)) * 0.6f, 0.6f, 0.9f);
                mr.sharedMaterial = new Material(platformMaterial);
                mr.sharedMaterial.color = c;
            }
            
            // === Create Hex Arena ===
            var arenaGO = new GameObject("HexArena");
            arenaGO.transform.SetParent(go.transform, false);

            var arena = arenaGO.AddComponent<HexBattleArenaGenerator>();
            arena.hexCellPrefab = Resources.Load<GameObject>("Prefabs/HexagonOutline");  // или свой путь
            arena.arenaEnabled = true;

            // world center of platform (top surface)
            Vector3 worldCenter = go.transform.position;
            arena.Init(outline, worldCenter);
        }
        
        // -------------------------------------------------------
        // Register graph in runtime registry
        // -------------------------------------------------------
        var reg = PlatformGraphRegistry.Instance;
        if (reg != null)
        {
            Debug.Log("Registering generated platform graph with PlatformGraphRegistry");
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                var nn = new PlatformGraphRegistry.Node();
                nn.id = i;
                nn.platform = n.go;
                nn.worldPos = n.go.transform.position;
                nn.topBoundary = n.topBoundary;
                reg.RegisterNode(nn);
            }

            // Add edges
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                var a = reg.nodes[e.a];
                var b = reg.nodes[e.b];

                a.neighbors.Add(b);
                b.neighbors.Add(a);
            }
        }
    }

    [ContextMenu("Clear Generated Graph")]
    public void ClearGraph()
    {
        // destroy parent if exists
        var existing = transform.Find(parentName);
        if (existing != null) { 
            #if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(existing.gameObject); else Destroy(existing.gameObject);
            #else
                Destroy(existing.gameObject);
            #endif
        }
        nodes.Clear(); edges.Clear(); parentGO = null;
    }

    // --------------------------- Mesh creation ---------------------------
    GameObject CreatePlatformMeshGO(int id, float length, float width, int vertexCount, float jitter, float thickness, out List<Vector3> outline)
    {
        Mesh m = BuildPlatformMesh(length, width, vertexCount, jitter, thickness, out outline);
        GameObject go = new GameObject($"Platform_{id}");
        var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = m;
        var mr = go.AddComponent<MeshRenderer>();
        var mc = go.AddComponent<MeshCollider>(); 
        mc.sharedMesh = null;
        mc.sharedMesh = m;        
        return go;
    }

    Mesh BuildPlatformMesh(float length, float width, int vertexCount, float jitter, float thickness, out List<Vector3> outline)
    {
        // Build an approximate 2D polygon top (centered at 0,0 in XZ plane), then extrude downwards by thickness
        // Steps:
        // 1) create radial points around ellipse of given length/width with jitter
        // 2) create top vertices (duplicate for flat shading) and bottom vertices
        // 3) triangulate top using fan from center (low-poly) — okay for mostly convex shapes
        // 4) create side faces connecting top and bottom loops

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
        // For low-poly flat shading we will duplicate vertices for top triangles and side quads
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        // Top: fan triangles (нормали вверх)
        int topStart = verts.Count;
        verts.Add(center); normals.Add(Vector3.up); uvs.Add(new Vector2(0.5f,0.5f));
        for (int i = 0; i < topVerts.Count; i++) { verts.Add(topVerts[i]); normals.Add(Vector3.up); uvs.Add(topUV[i]); }

        // Проверяем порядок: против часовой стрелки для верхней грани
        for (int i = 0; i < topVerts.Count; i++)
        {
            int aIdx = topStart + 1 + i;
            int bIdx = topStart + 1 + ((i + 1) % topVerts.Count);
            tris.Add(topStart); tris.Add(bIdx); tris.Add(aIdx); // поменяли a <-> b
        }

        // Bottom verts (flat bottom at -thickness)
        int bottomStart = verts.Count;
        for (int i = 0; i < topVerts.Count; i++) { 
            verts.Add(new Vector3(topVerts[i].x, -thickness, topVerts[i].z)); 
            normals.Add(Vector3.down); 
            uvs.Add(topUV[i]); 
        }

        // Bottom triangulation (reverse winding для нормалей вниз)
        for (int i = 0; i < topVerts.Count - 2; i++)
        {
            tris.Add(bottomStart + 0);
            tris.Add(bottomStart + i + 1);
            tris.Add(bottomStart + i + 2); // поменяли порядок на 0, i+1, i+2
        }


        // Sides: quads per edge (as two triangles). We'll use duplicated vertices per face for flat shading
        for (int i = 0; i < topVerts.Count; i++)
        {
            int ni = (i + 1) % topVerts.Count;
            Vector3 vTopA = topVerts[i]; Vector3 vTopB = topVerts[ni];
            Vector3 vBotA = new Vector3(vTopA.x, -thickness, vTopA.z); Vector3 vBotB = new Vector3(vTopB.x, -thickness, vTopB.z);
            int s0 = verts.Count; verts.Add(vTopA); verts.Add(vTopB); verts.Add(vBotB); verts.Add(vBotA);
            // normal for this face
            Vector3 faceNormal = Vector3.Cross(vTopB - vTopA, vBotA - vTopA).normalized;
            normals.Add(faceNormal); normals.Add(faceNormal); normals.Add(faceNormal); normals.Add(faceNormal);
            uvs.Add(new Vector2(0,0)); uvs.Add(new Vector2(1,0)); uvs.Add(new Vector2(1,1)); uvs.Add(new Vector2(0,1));
            // two triangles
            tris.Add(s0 + 0); tris.Add(s0 + 1); tris.Add(s0 + 2);
            tris.Add(s0 + 0); tris.Add(s0 + 2); tris.Add(s0 + 3);
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

    // --------------------------- Utility ---------------------------
    //Vector2 CellCenter(Vector2Int c) { return new Vector2(c.x + 0.5f, c.y + 0.5f); }

    /*Vector2 SegmentCenterToWorld(int segX, int segY, int gridSize)
    {
        ComputeMapDims();
        int segW = Mathf.FloorToInt(mapWidth / (float)gridSize);
        int segH = Mathf.FloorToInt(mapHeight / (float)gridSize);
        int sx = Mathf.Clamp(segX * segW, 0, mapWidth - 1);
        int sy = Mathf.Clamp(segY * segH, 0, mapHeight - 1);
        int ex = Mathf.Min(mapWidth - 1, sx + segW - 1);
        int ey = Mathf.Min(mapHeight - 1, sy + segH - 1);
        int cx = (sx + ex) / 2; int cy = (sy + ey) / 2; return CellCenter(cx, cy);
    }*/

    /*void ComputeMapDims()
    {
        mapWidth = Mathf.Clamp(baseGridSize, 8, 4096);
        float ratio = Mathf.Clamp(aspect.y / aspect.x, 0.05f, 20f);
        mapHeight = Mathf.Clamp(Mathf.RoundToInt(mapWidth * ratio), 8, 4096);
    }*/

    // --------------------------- Editor convenience ---------------------------
#if UNITY_EDITOR
    [CustomEditor(typeof(ProceduralPlatformGraphGenerator))]
    public class ProceduralPlatformGraphGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ProceduralPlatformGraphGenerator g = (ProceduralPlatformGraphGenerator)target;
            GUILayout.Space(8);
            if (GUILayout.Button("Generate")) { g.GenerateGraph(); }
            if (GUILayout.Button("Clear")) { g.ClearGraph(); }
        }
    }
#endif

    // --------------------------- Gizmos ---------------------------
    void OnDrawGizmos()
    {
        if (!visualizeGizmos) return;
        Gizmos.color = Color.yellow;
        if (nodes != null)
        {
            foreach (var n in nodes) Gizmos.DrawWireSphere(transform.TransformPoint(n.position), 0.1f);
        }
        if (edges != null)
        {
            foreach (var e in edges)
            {
                if (e.a >= 0 && e.a < nodes.Count && e.b >= 0 && e.b < nodes.Count)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.TransformPoint(nodes[e.a].position), transform.TransformPoint(nodes[e.b].position));
                }
            }
        }
    }
    
    // --------------------------- Platform Collider Builder ---------------------------
    public class PlatformColliderBuilder
    {
        public static void BuildPlatformColliders(
            GameObject platformObj,
            List<Vector3> outlinePoints, // XZ points describing platform perimeter
            float floorThickness = 0.1f,
            float wallHeight = 2.0f,
            float wallThickness = 0.2f)
        {
            // MAIN HOLDER
            GameObject root = new GameObject("PlatformCollider");
            root.transform.SetParent(platformObj.transform, false);
            root.transform.localPosition = Vector3.zero;
    
            // ======================
            // 1. FLOOR COLLIDER
            // ======================
            var floor = new GameObject("FloorCollider");
            floor.transform.SetParent(root.transform, false);
    
            var floorCol = floor.AddComponent<BoxCollider>();
            Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, 0, float.MinValue);

            foreach (var p in outlinePoints)
            {
                if (p.x < min.x) min.x = p.x;
                if (p.z < min.z) min.z = p.z;
                if (p.x > max.x) max.x = p.x;
                if (p.z > max.z) max.z = p.z;
            }

            Vector3 size = max - min;
            Vector3 center = (max + min) * 0.5f;

            floorCol.size = new Vector3(size.x, floorThickness, size.z);
            floorCol.center = new Vector3(center.x, -floorThickness * 0.5f, center.z);

    
            // ======================
            // 2. PERIMETER COLLIDERS
            // ======================
    
            // Each segment between outline points
            for (int i = 0; i < outlinePoints.Count; i++)
            {
                Vector3 a = outlinePoints[i];
                Vector3 b = outlinePoints[(i + 1) % outlinePoints.Count];
    
                Vector3 mid = (a + b) * 0.5f;
    
                float segmentLength = Vector3.Distance(a, b);
    
                GameObject wall = new GameObject($"WallCollider_{i}");
                wall.layer = LayerMask.NameToLayer("WallLayer");
                wall.transform.SetParent(root.transform, false);
    
                // Move to middle of the segment
                wall.transform.localPosition = new Vector3(mid.x, wallHeight * 0.5f, mid.z);
    
                // Rotate the wall to align with the edge
                Vector3 dir = (b - a).normalized;
                wall.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    
                var wallCol = wall.AddComponent<BoxCollider>();
                wallCol.size = new Vector3(wallThickness, wallHeight, segmentLength);
            }
        }
    }
    
}
