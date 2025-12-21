// TerrainMapEditorAndGenerator.cs
// Editor + runtime implementation to design a grid-based map with a winding main road and dead-end plateau branches,
// and to generate a flat Unity Terrain colored by base/road/plateau layers.
// No ScriptableObjects; configuration is editable in the EditorWindow and in a runtime MonoBehaviour (Inspector).

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMapGeneratorComponent : MonoBehaviour
{
    [Header("Map Shape")]
    public int baseGridSize = 128; // number of columns for square-ish map
    public Vector2 aspect = new Vector2(1f, 1f); // X:Y ratio

    [Header("Road Config")]
    public Color roadColor = new Color(0.35f,0.2f,0.1f);
    public enum Edge { Left, Right, Top, Bottom }
    public Edge startEdge = Edge.Left;
    [Range(0f,1f)] public float startEdgePos01 = 0.5f;
    public Edge endEdge = Edge.Right;
    [Range(0f,1f)] public float endEdgePos01 = 0.5f;
    [Range(1,12)] public int roadWidthCells = 4;
    [Range(0f,1f)] public float sinuosity = 0.35f;
    [Range(3,32)] public int passEveryN = 9;

    [Header("Plateaus (ordered list)")]
    public List<PlateauSpec> plateaus = new List<PlateauSpec>() { new PlateauSpec(), new PlateauSpec(), new PlateauSpec() };

    [Header("Misc")]
    public int seed = 12345;
    public int previewTileSize = 4; // pixels per cell in preview

    // Generated map data (2D arrays)
    [NonSerialized] public int mapWidth = 128;
    [NonSerialized] public int mapHeight = 128;
    [NonSerialized] public int[,] mapMask; // 0 empty, 1 road, 2..N plateau id

    [Serializable]
    public class PlateauSpec { public int radiusCells = 4; public Color color = Color.yellow; }

    // Generate map data and also build terrain
    public void GenerateMapData()
    {
        UnityEngine.Random.InitState(seed);
        ComputeMapDims();
        mapMask = new int[mapWidth, mapHeight];
        for (int y = 0; y < mapHeight; y++) for (int x = 0; x < mapWidth; x++) mapMask[x,y] = 0;

        Vector2Int start = PickCellOnEdge(startEdge, startEdgePos01);
        Vector2Int end = PickCellOnEdge(endEdge, endEdgePos01);

        List<Vector2> control = BuildSinuousControlPoints(start, end);
        List<Vector2> sampled = SamplePolyline(control, Mathf.Max(mapWidth,mapHeight)*4);

        // rasterize main road
        RasterizePathToGrid(sampled, roadWidthCells, 1);

        // place plateaus along path in order
        int platoId = 2;
        for (int i = 0; i < plateaus.Count; i++)
        {
            float t = (i + 1f) / (plateaus.Count + 1f);
            Vector2 pos = LerpAlongPath(sampled, t);
            Vector2 tangent = EstimateTangent(sampled, pos);
            Vector2 perp = new Vector2(-tangent.y, tangent.x).normalized;
            if (UnityEngine.Random.value > 0.5f) perp = -perp;
            float branchLen = Mathf.Max(3f, plateaus[i].radiusCells * 3f);
            Vector2 branchEnd = pos + perp * branchLen;
            Vector2Int branchEndCell = WorldToCellNearest(branchEnd);
            List<Vector2> branchLine = SampleLine(CellToWorldNearest(WorldToCellNearest(pos)), CellToWorldNearest(branchEndCell), Mathf.CeilToInt(branchLen*4f));
            RasterizePathToGrid(branchLine, Mathf.Max(1, roadWidthCells-1), 1);
            FillCircleOnGrid(branchEndCell.x, branchEndCell.y, plateaus[i].radiusCells, platoId);
            platoId++;
        }
    }

    public void GenerateTerrainInScene(bool destroyExisting = true)
    {
        if (mapMask == null) GenerateMapData();
        // create TerrainData
        TerrainData td = new TerrainData();
        int hmRes = Mathf.NextPowerOfTwo(Mathf.Max(mapWidth, mapHeight)) + 1;
        hmRes = Mathf.Clamp(hmRes, 33, 4097);
        td.heightmapResolution = hmRes;
        td.size = new Vector3(mapWidth, 5f, mapHeight);
        float[,] heights = new float[td.heightmapResolution, td.heightmapResolution];
        for (int y = 0; y < td.heightmapResolution; y++) for (int x = 0; x < td.heightmapResolution; x++) heights[y,x] = 0f; // flat
        td.SetHeights(0,0,heights);

        // TerrainLayers: base, road, one plato layer (we will show all plateaus with same layer but different colors by combining later via splat priorities)
        TerrainLayer baseLayer = MakeColorTerrainLayer("BaseLayer", Color.Lerp(Color.green, Color.gray, 0.2f));
        TerrainLayer roadLayer = MakeColorTerrainLayer("RoadLayer", roadColor);
        TerrainLayer platoLayer = MakeColorTerrainLayer("PlatoLayer", plateaus.Count>0?plateaus[0].color:Color.yellow);
        td.terrainLayers = new TerrainLayer[]{ baseLayer, roadLayer, platoLayer };

        // alphamap (splat) resolution
        int alphaRes = Mathf.Clamp(Mathf.Max(mapWidth, mapHeight), 32, 1024);
        td.alphamapResolution = alphaRes;
        int numLayers = td.terrainLayers.Length;
        float[,,] alphas = new float[alphaRes, alphaRes, numLayers];
        for (int ay = 0; ay < alphaRes; ay++) for (int ax = 0; ax < alphaRes; ax++)
        {
            float u = (float)ax / (alphaRes-1); float v = (float)ay / (alphaRes-1);
            int gx = Mathf.Clamp(Mathf.RoundToInt(u*(mapWidth-1)), 0, mapWidth-1);
            int gy = Mathf.Clamp(Mathf.RoundToInt(v*(mapHeight-1)), 0, mapHeight-1);
            // priority: plateau > road > base
            if (mapMask[gx,gy] >= 2) { alphas[ay,ax,2] = 1f; }
            else if (mapMask[gx,gy] == 1) { alphas[ay,ax,1] = 1f; }
            else { alphas[ay,ax,0] = 1f; }
        }
        td.SetAlphamaps(0,0,alphas);

        // create or replace GameObject terrain
        GameObject existing = GameObject.Find("ProceduralTerrain");
        if (existing != null && destroyExisting) DestroyImmediate(existing);
        GameObject root = new GameObject("ProceduralTerrain");
        Terrain t = root.AddComponent<Terrain>(); TerrainCollider tc = root.AddComponent<TerrainCollider>();
        t.terrainData = td; tc.terrainData = td;

        // set material to default if missing (avoid pink)
#if UNITY_EDITOR
        if (t.materialTemplate == null)
        {
            var defaultMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Default-Terrain-Material.mat");
            if (defaultMat != null) t.materialTemplate = defaultMat;
        }
#endif
    }

    TerrainLayer MakeColorTerrainLayer(string name, Color color)
    {
        TerrainLayer layer = new TerrainLayer(); layer.name = name;
        Texture2D tex = new Texture2D(1,1); tex.SetPixel(0,0,color); tex.Apply();
        layer.diffuseTexture = tex;
        return layer;
    }

    // ---- helper grid/world conversions ----
    void ComputeMapDims()
    {
        mapWidth = Mathf.Clamp(baseGridSize, 8, 1024);
        float ratio = Mathf.Clamp(aspect.y / aspect.x, 0.1f, 10f);
        mapHeight = Mathf.Clamp(Mathf.RoundToInt(mapWidth * ratio), 8, 1024);
    }

    Vector2Int PickCellOnEdge(Edge e, float frac01)
    {
        frac01 = Mathf.Clamp01(frac01);
        ComputeMapDims();
        if (e == Edge.Left) return new Vector2Int(0, Mathf.RoundToInt(frac01*(mapHeight-1)));
        if (e == Edge.Right) return new Vector2Int(mapWidth-1, Mathf.RoundToInt(frac01*(mapHeight-1)));
        if (e == Edge.Top) return new Vector2Int(Mathf.RoundToInt(frac01*(mapWidth-1)), mapHeight-1);
        return new Vector2Int(Mathf.RoundToInt(frac01*(mapWidth-1)), 0);
    }

    Vector2 CellToWorld(int x, int y)
    {
        return new Vector2(x + 0.5f, y + 0.5f);
    }
    Vector2 CellToWorldNearest(Vector2Int c) { return CellToWorld(c.x, c.y); }
    Vector2Int WorldToCellNearest(Vector2 w)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(w.x-0.5f), 0, mapWidth-1);
        int y = Mathf.Clamp(Mathf.RoundToInt(w.y-0.5f), 0, mapHeight-1);
        return new Vector2Int(x,y);
    }

    // ---- path generation & rasterization (grid-based) ----
    List<Vector2> BuildSinuousControlPoints(Vector2Int start, Vector2Int end)
    {
        List<Vector2> cp = new List<Vector2>();
        Vector2 ws = CellToWorldNearest(start); Vector2 we = CellToWorldNearest(end);
        cp.Add(ws);
        int segments = Mathf.Max(3, Mathf.RoundToInt((Mathf.Max(mapWidth,mapHeight)/ (float)passEveryN) * 2));
        for (int i=1;i<=segments;i++)
        {
            float t = i/(float)(segments+1);
            Vector2 p = Vector2.Lerp(ws, we, t);
            Vector2 baseDir = (we - ws).normalized; Vector2 perp = new Vector2(-baseDir.y, baseDir.x);
            float noise = (Mathf.PerlinNoise(t*10f + seed*0.001f, t*10f + 1.23f)-0.5f)*2f;
            float amplitude = sinuosity * Mathf.Max(mapWidth,mapHeight) * 0.2f;
            p += perp * noise * amplitude;
            cp.Add(p);
        }
        cp.Add(we);
        return cp;
    }

    List<Vector2> SamplePolyline(List<Vector2> pts, int sampleCount)
    {
        List<Vector2> outp = new List<Vector2>(); if (pts.Count<2) return outp;
        float total = 0f; float[] segLen = new float[pts.Count-1];
        for (int i=0;i<pts.Count-1;i++){ segLen[i] = Vector2.Distance(pts[i], pts[i+1]); total += segLen[i]; }
        if (total <= 0){ outp.AddRange(pts); return outp; }
        for (int s=0;s<sampleCount;s++){
            float u = s/(float)(sampleCount-1); float dist = u*total; float acc = 0f; int seg = 0; while(seg<segLen.Length && acc+segLen[seg]<dist){ acc+=segLen[seg]; seg++; }
            if(seg>=segLen.Length){ outp.Add(pts[pts.Count-1]); continue; }
            float localT = (dist-acc)/Mathf.Max(1e-5f, segLen[seg]); outp.Add(Vector2.Lerp(pts[seg], pts[seg+1], localT));
        }
        return outp;
    }

    List<Vector2> SampleLine(Vector2 a, Vector2 b, int samples){ List<Vector2> outp = new List<Vector2>(); for(int i=0;i<samples;i++) outp.Add(Vector2.Lerp(a,b,i/(float)(samples-1))); return outp; }

    void RasterizePathToGrid(List<Vector2> sampledWorld, int widthCells, int markValue)
    {
        if (sampledWorld==null || sampledWorld.Count==0) return;
        int r = Mathf.Max(1, widthCells);
        foreach (var p in sampledWorld)
        {
            Vector2Int c = WorldToCellNearest(p);
            FillCircleOnGrid(c.x, c.y, r, markValue);
        }
    }

    void FillCircleOnGrid(int cx, int cy, int radius, int value)
    {
        for(int dy=-radius; dy<=radius; dy++) for(int dx=-radius; dx<=radius; dx++){
            int nx = cx+dx, ny = cy+dy; if (nx<0||ny<0||nx>=mapWidth||ny>=mapHeight) continue;
            if (dx*dx+dy*dy <= radius*radius) mapMask[nx,ny] = value;
        }
    }

    float[,] tempFloat; // helper

    Vector2 LerpAlongPath(List<Vector2> path, float t)
    {
        if (path==null || path.Count==0) return Vector2.zero; if (t<=0) return path[0]; if(t>=1) return path[path.Count-1];
        float total=0; float[] seg = new float[path.Count-1]; for(int i=0;i<path.Count-1;i++){ seg[i]=Vector2.Distance(path[i],path[i+1]); total+=seg[i]; }
        float dist = t*total; float acc=0; for(int i=0;i<seg.Length;i++){ if(acc+seg[i]>=dist){ float lt=(dist-acc)/seg[i]; return Vector2.Lerp(path[i],path[i+1],lt);} acc+=seg[i]; }
        return path[path.Count-1];
    }

    Vector2 EstimateTangent(List<Vector2> path, Vector2 at)
    {
        int best=0; float bd=float.MaxValue; for(int i=0;i<path.Count-1;i++){ Vector2 a=path[i], b=path[i+1]; Vector2 proj = ProjectPointOnSegment(at,a,b); float d=Vector2.SqrMagnitude(at-proj); if(d<bd){bd=d; best=i;} }
        Vector2 dir = (path[Mathf.Clamp(best+1,0,path.Count-1)] - path[best]).normalized; return dir;
    }

    Vector2 ProjectPointOnSegment(Vector2 p, Vector2 a, Vector2 b){ Vector2 ap=p-a; Vector2 ab=b-a; float ab2=Vector2.Dot(ab,ab); if(ab2==0) return a; float t=Mathf.Clamp01(Vector2.Dot(ap,ab)/ab2); return a+ab*t; }

    // ---- Utility to expose regeneration from inspector at runtime
    public void RegenerateAndApplyTerrain()
    {
        GenerateMapData();
        GenerateTerrainInScene(true);
    }
}

#if UNITY_EDITOR
public class TerrainMapEditorWindow : EditorWindow
{
    TerrainMapGeneratorComponent config;
    Texture2D previewTex;

    [MenuItem("Window/Terrain Map Designer")]
    public static void ShowWindow(){ GetWindow<TerrainMapEditorWindow>("Terrain Map Designer"); }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Terrain Map Designer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        config = EditorGUILayout.ObjectField("Target Generator (Component)", config, typeof(TerrainMapGeneratorComponent), true) as TerrainMapGeneratorComponent;
        if (config == null)
        {
            EditorGUILayout.HelpBox("Create an empty GameObject and add TerrainMapGeneratorComponent to it, then assign it here (or create new).", MessageType.Info);
            if (GUILayout.Button("Create New Generator GameObject")) CreateNewGenerator();
            return;
        }

        // Show and edit fields from component (automatic inspector-like)
        SerializedObject so = new SerializedObject(config);
        so.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(so.FindProperty("baseGridSize"));
        EditorGUILayout.PropertyField(so.FindProperty("aspect"));
        EditorGUILayout.PropertyField(so.FindProperty("seed"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Road", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("startEdge"));
        EditorGUILayout.PropertyField(so.FindProperty("startEdgePos01"));
        EditorGUILayout.PropertyField(so.FindProperty("endEdge"));
        EditorGUILayout.PropertyField(so.FindProperty("endEdgePos01"));
        EditorGUILayout.PropertyField(so.FindProperty("roadColor"));
        EditorGUILayout.PropertyField(so.FindProperty("roadWidthCells"));
        EditorGUILayout.PropertyField(so.FindProperty("sinuosity"));
        EditorGUILayout.PropertyField(so.FindProperty("passEveryN"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Plateaus (ordered)", EditorStyles.boldLabel);
        SerializedProperty plateausProp = so.FindProperty("plateaus");
        EditorGUILayout.PropertyField(plateausProp, true);
        EditorGUILayout.Space();
        if (EditorGUI.EndChangeCheck()) { so.ApplyModifiedProperties(); RebuildPreview(); }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Preview")) { config.GenerateMapData(); RebuildPreview(); }
        if (GUILayout.Button("Apply to Terrain (create)")) { config.GenerateMapData(); config.GenerateTerrainInScene(true); }
        if (GUILayout.Button("Apply to Terrain (replace)")) { config.GenerateMapData(); config.GenerateTerrainInScene(true); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        if (previewTex != null) GUILayout.Label(previewTex, GUILayout.Width(512), GUILayout.Height(512 * ((float)config.mapHeight/config.mapWidth)));
        else EditorGUILayout.HelpBox("No preview yet. Click Generate Preview.", MessageType.Info);

        GUILayout.FlexibleSpace();
    }

    void CreateNewGenerator()
    {
        GameObject go = new GameObject("TerrainMapGenerator");
        config = go.AddComponent<TerrainMapGeneratorComponent>();
        Selection.activeGameObject = go;
    }

    void RebuildPreview()
    {
        if (config == null) return; if (config.mapMask == null) config.GenerateMapData();
        int w = config.mapWidth, h = config.mapHeight; int pxW = Mathf.Clamp(w*config.previewTileSize, 64, 2048); int pxH = Mathf.Clamp(h*config.previewTileSize, 64, 2048);
        previewTex = new Texture2D(pxW, pxH, TextureFormat.RGBA32, false);
        Color baseC = Color.Lerp(Color.green, Color.gray, 0.2f);
        for (int y = 0; y < pxH; y++) for (int x = 0; x < pxW; x++) previewTex.SetPixel(x,y, baseC);
        for (int gy=0; gy<h; gy++) for (int gx=0; gx<w; gx++)
        {
            int val = config.mapMask[gx,gy]; Color c = baseC;
            if (val == 1) c = config.roadColor; else if (val >= 2) { int idx = val-2; if (idx>=0 && idx<config.plateaus.Count) c = config.plateaus[idx].color; else c = Color.yellow; }
            int cx = Mathf.RoundToInt(gx*(pxW-1)/(float)(w-1)); int cy = Mathf.RoundToInt(gy*(pxH-1)/(float)(h-1));
            // fill a block of size tile
            int tile = config.previewTileSize; for (int yy = cy; yy<cy+tile && yy<pxH; yy++) for (int xx = cx; xx<cx+tile && xx<pxW; xx++) previewTex.SetPixel(xx,yy,c);
        }
        previewTex.Apply();
        Repaint();
    }
}
#endif
