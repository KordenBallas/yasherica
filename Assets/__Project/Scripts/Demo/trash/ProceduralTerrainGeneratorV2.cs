#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteAlways]
public class ProceduralTerrainGeneratorV2 : MonoBehaviour
{
    [Header("Map Shape")]
    public int baseGridSize = 128;
    public Vector2 aspect = new Vector2(1f,1f);

    [Header("Road")]
    public Color roadColor = new Color(0.35f,0.2f,0.1f);
    public enum Edge { Left, Right, Top, Bottom }
    public Edge startEdge = Edge.Left;
    [Range(0f,1f)] public float startEdgePos01 = 0.5f;
    public Edge endEdge = Edge.Right;
    [Range(0f,1f)] public float endEdgePos01 = 0.5f;
    [Range(1,24)] public int roadWidthCells = 3;
    [Range(0f,1f)] public float sinuosity = 0.35f;
    [Range(3,64)] public int passEveryN = 9;

    [Header("Plateaus")]
    public List<PlateauSpec> plateaus = new List<PlateauSpec>(){ new PlateauSpec(), new PlateauSpec(), new PlateauSpec() };

    [Header("Misc")]
    public int seed = 12345;
    public int previewPixelPerCell = 4;

    [NonSerialized] public int mapWidth = 128;
    [NonSerialized] public int mapHeight = 128;
    [NonSerialized] public int[,] mapMask;

    [Serializable]
    public class PlateauSpec { public int radiusCells = 4; public Color color = Color.yellow; }

    const string generatedFolder = "Assets/ProceduralTerrainGenerated";

    // -----------------------
    // MAP GENERATION
    // -----------------------
    public void GenerateMapMask()
    {
        UnityEngine.Random.InitState(seed);
        ComputeMapDims();
        mapMask = new int[mapWidth,mapHeight];
        for(int y=0;y<mapHeight;y++) for(int x=0;x<mapWidth;x++) mapMask[x,y]=0;

        Vector2Int start = PickCellOnEdge(startEdge,startEdgePos01);
        Vector2Int end = PickCellOnEdge(endEdge,endEdgePos01);

        int gridSize = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(passEveryN)),2,64);
        List<Vector2> segmentCenters = ComputeSegmentCenters(gridSize);

        // Build path points
        List<Vector2> pathPoints = new List<Vector2>();
        pathPoints.Add(CellCenter(start));
        for(int row=0;row<gridSize;row++)
        {
            if(row%2==0) for(int col=0;col<gridSize;col++) pathPoints.Add(SegmentCenterToWorld(col,row,gridSize));
            else for(int col=gridSize-1;col>=0;col--) pathPoints.Add(SegmentCenterToWorld(col,row,gridSize));
        }
        pathPoints.Add(CellCenter(end));

        // add sinuosity
        List<Vector2> controlPts = new List<Vector2>();
        controlPts.Add(pathPoints[0]);
        for(int i=1;i<pathPoints.Count-1;i++)
        {
            Vector2 prev = pathPoints[Mathf.Max(0,i-1)];
            Vector2 next = pathPoints[Mathf.Min(pathPoints.Count-1,i+1)];
            Vector2 baseDir = (next-prev).normalized;
            Vector2 perp = new Vector2(-baseDir.y, baseDir.x);
            float t = i/(float)(pathPoints.Count-1);
            float noise = (Mathf.PerlinNoise(t*5f+seed*0.001f,t*7.3f)-0.5f)*2f;
            float amp = sinuosity*Mathf.Max(mapWidth,mapHeight)*0.25f;
            controlPts.Add(pathPoints[i]+perp*noise*amp);
        }
        controlPts.Add(pathPoints[pathPoints.Count-1]);

        List<Vector2> sampled = SamplePolyline(controlPts,Mathf.Max(mapWidth,mapHeight)*6);
        RasterizePathToGrid(sampled,roadWidthCells,1);

        // plateaus
        int plateauId=2;
        int segmentsTotal = gridSize*gridSize;
        for(int i=0;i<plateaus.Count;i++)
        {
            int segIndex = Mathf.Clamp(Mathf.RoundToInt((i+1f)/(plateaus.Count+1f)*segmentsTotal)-1,0,segmentsTotal-1);
            int sx = segIndex%gridSize; int sy = segIndex/gridSize;
            Vector2 segWorld = SegmentCenterToWorld(sx,sy,gridSize);
            Vector2 tangent = EstimateTangent(sampled,segWorld);
            Vector2 perp = new Vector2(-tangent.y,tangent.x).normalized;
            if(UnityEngine.Random.value>0.5f) perp=-perp;
            float branchLen = Mathf.Max(3f,plateaus[i].radiusCells*3f);
            Vector2 branchEnd = segWorld+perp*branchLen;
            Vector2Int branchEndCell = WorldToCellNearest(branchEnd);
            List<Vector2> branchLine = SampleLine(CellCenter(WorldToCellNearest(segWorld)),CellCenter(branchEndCell),Mathf.CeilToInt(branchLen*4f));
            RasterizePathToGrid(branchLine,Mathf.Max(1,roadWidthCells-1),1);
            FillCircleOnGrid(branchEndCell.x,branchEndCell.y,plateaus[i].radiusCells,plateauId);
            plateauId++;
        }
    }

    // -----------------------
    // TERRAIN GENERATION
    // -----------------------
    public void GenerateTerrainFromMask(bool destroyExisting=true)
    {
        if(mapMask==null) GenerateMapMask();

#if UNITY_EDITOR
        if(!AssetDatabase.IsValidFolder(generatedFolder)) AssetDatabase.CreateFolder("Assets","ProceduralTerrainGenerated");
#endif

        TerrainData td = new TerrainData();
        int hmRes = Mathf.NextPowerOfTwo(Mathf.Max(mapWidth,mapHeight))+1;
        hmRes = Mathf.Clamp(hmRes,33,4097);
        td.heightmapResolution = hmRes;
        td.size = new Vector3(mapWidth,10f,mapHeight);
        float[,] heights = new float[td.heightmapResolution,td.heightmapResolution];
        td.SetHeights(0,0,heights);

        // layers
        List<TerrainLayer> layers = new List<TerrainLayer>();
        TerrainLayer baseLayer = MakeAndSaveTerrainLayer("BaseLayer",Color.Lerp(Color.green,Color.gray,0.2f));
        TerrainLayer roadLayer = MakeAndSaveTerrainLayer("RoadLayer",roadColor);
        layers.Add(baseLayer); layers.Add(roadLayer);
        List<TerrainLayer> plateauLayers = new List<TerrainLayer>();
        for(int i=0;i<plateaus.Count;i++)
        {
            var pl = MakeAndSaveTerrainLayer($"PlatoLayer_{i+1}",plateaus[i].color);
            plateauLayers.Add(pl);
            layers.Add(pl);
        }
        td.terrainLayers = layers.ToArray();

        // alphamap
        int alphaRes = Mathf.Clamp(Mathf.Max(mapWidth,mapHeight),32,2048);
        td.alphamapResolution = alphaRes;
        int numLayers = td.terrainLayers.Length;
        float[,,] alphas = new float[alphaRes,alphaRes,numLayers];
        for(int ay=0;ay<alphaRes;ay++) for(int ax=0;ax<alphaRes;ax++)
        {
            float u = (float)ax/(alphaRes-1); float v = (float)ay/(alphaRes-1);
            int gx = Mathf.Clamp(Mathf.RoundToInt(u*(mapWidth-1)),0,mapWidth-1);
            int gy = Mathf.Clamp(Mathf.RoundToInt(v*(mapHeight-1)),0,mapHeight-1);
            int maskVal = mapMask[gx,gy];
            for(int li=0;li<numLayers;li++) alphas[ay,ax,li]=0f;
            if(maskVal>=2)
            {
                int pid=maskVal-2; int li=2+pid; if(li<numLayers) alphas[ay,ax,li]=1f; else alphas[ay,ax,0]=1f;
            }
            else if(maskVal==1) alphas[ay,ax,1]=1f;
            else alphas[ay,ax,0]=1f;
        }
        td.SetAlphamaps(0,0,alphas);

        GameObject existing = GameObject.Find("ProceduralTerrain");
        if(existing!=null && destroyExisting) DestroyImmediate(existing);
        GameObject root = new GameObject("ProceduralTerrain");
        Terrain terrain = root.AddComponent<Terrain>();
        TerrainCollider tc = root.AddComponent<TerrainCollider>();
        terrain.terrainData = td; tc.terrainData = td;
    }

    // -----------------------
    // HELPERS: Grid/World
    // -----------------------
    void ComputeMapDims(){ mapWidth = Mathf.Clamp(baseGridSize,8,2048); mapHeight = Mathf.Clamp(Mathf.RoundToInt(mapWidth*aspect.y/aspect.x),8,2048); }
    Vector2 CellCenter(Vector2Int c){ return new Vector2(c.x+0.5f,c.y+0.5f); }
    Vector2 CellCenter(int x,int y){ return new Vector2(x+0.5f,y+0.5f); }
    Vector2Int WorldToCellNearest(Vector2 w){ return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(w.x-0.5f),0,mapWidth-1),Mathf.Clamp(Mathf.RoundToInt(w.y-0.5f),0,mapHeight-1)); }
    Vector2Int PickCellOnEdge(Edge e,float f){ f=Mathf.Clamp01(f); ComputeMapDims(); if(e==Edge.Left) return new Vector2Int(0,Mathf.RoundToInt(f*(mapHeight-1))); if(e==Edge.Right) return new Vector2Int(mapWidth-1,Mathf.RoundToInt(f*(mapHeight-1))); if(e==Edge.Top) return new Vector2Int(Mathf.RoundToInt(f*(mapWidth-1)),mapHeight-1); return new Vector2Int(Mathf.RoundToInt(f*(mapWidth-1)),0); }

    List<Vector2> ComputeSegmentCenters(int gridSize)
    {
        List<Vector2> centers = new List<Vector2>();
        int segW = Mathf.FloorToInt(mapWidth/(float)gridSize);
        int segH = Mathf.FloorToInt(mapHeight/(float)gridSize);
        for(int ry=0;ry<gridSize;ry++) for(int rx=0;rx<gridSize;rx++)
        {
            int sx=rx*segW; int sy=ry*segH; int ex=Mathf.Min(mapWidth-1,sx+segW-1); int ey=Mathf.Min(mapHeight-1,sy+segH-1);
            int cx=(sx+ex)/2; int cy=(sy+ey)/2;
            centers.Add(CellCenter(cx,cy));
        }
        return centers;
    }

    Vector2 SegmentCenterToWorld(int segX,int segY,int gridSize)
    {
        int segW = Mathf.FloorToInt(mapWidth/(float)gridSize);
        int segH = Mathf.FloorToInt(mapHeight/(float)gridSize);
        int sx = segX*segW; int sy = segY*segH; int ex = Mathf.Min(mapWidth-1,sx+segW-1); int ey = Mathf.Min(mapHeight-1,sy+segH-1);
        int cx = (sx+ex)/2; int cy = (sy+ey)/2;
        return CellCenter(cx,cy);
    }

    List<Vector2> SamplePolyline(List<Vector2> pts,int sampleCount)
    {
        List<Vector2> outp = new List<Vector2>();
        if(pts==null||pts.Count<2) return outp;
        float total=0f; float[] segLen=new float[pts.Count-1];
        for(int i=0;i<pts.Count-1;i++){ segLen[i]=Vector2.Distance(pts[i],pts[i+1]); total+=segLen[i]; }
        if(total<=0){ outp.AddRange(pts); return outp; }
        for(int s=0;s<sampleCount;s++)
        {
            float u=s/(float)Math.Max(1,sampleCount-1); float dist=u*total; float acc=0f; int seg=0;
            while(seg<segLen.Length && acc+segLen[seg]<dist){ acc+=segLen[seg]; seg++; }
            if(seg>=segLen.Length){ outp.Add(pts[pts.Count-1]); continue; }
            float localT=(dist-acc)/Math.Max(1e-6f,segLen[seg]);
            outp.Add(Vector2.Lerp(pts[seg],pts[seg+1],localT));
        }
        return outp;
    }

    List<Vector2> SampleLine(Vector2 a,Vector2 b,int samples){ List<Vector2> outp=new List<Vector2>(); for(int i=0;i<samples;i++) outp.Add(Vector2.Lerp(a,b,i/(float)(samples-1))); return outp; }
    void RasterizePathToGrid(List<Vector2> sampled,int widthCells,int mark){ if(sampled==null||sampled.Count==0) return; int r=Math.Max(1,widthCells); foreach(var p in sampled){ Vector2Int c=WorldToCellNearest(p); FillCircleOnGrid(c.x,c.y,r,mark); } }
    void FillCircleOnGrid(int cx,int cy,int r,int v){ for(int dy=-r;dy<=r;dy++) for(int dx=-r;dx<=r;dx++){ int nx=cx+dx, ny=cy+dy; if(nx<0||ny<0||nx>=mapWidth||ny>=mapHeight) continue; if(dx*dx+dy*dy<=r*r) mapMask[nx,ny]=v; } }

    Vector2 EstimateTangent(List<Vector2> path,Vector2 at){ if(path==null||path.Count<2) return Vector2.right; int best=0; float bd=float.MaxValue; for(int i=0;i<path.Count-1;i++){ Vector2 proj=ProjectPointOnSegment(at,path[i],path[i+1]); float d=Vector2.SqrMagnitude(at-proj); if(d<bd){bd=d; best=i;} } return (path[Mathf.Clamp(best+1,0,path.Count-1)]-path[best]).normalized; }
    Vector2 ProjectPointOnSegment(Vector2 p,Vector2 a,Vector2 b){ Vector2 ap=p-a; Vector2 ab=b-a; float ab2=Vector2.Dot(ab,ab); if(ab2==0) return a; float t=Mathf.Clamp01(Vector2.Dot(ap,ab)/ab2); return a+ab*t; }

    // -----------------------
    // TERRAIN LAYER CREATION
    // -----------------------
#if UNITY_EDITOR
    TerrainLayer MakeAndSaveTerrainLayer(string name, Color color)
    {
        if(!AssetDatabase.IsValidFolder(generatedFolder)) AssetDatabase.CreateFolder("Assets","ProceduralTerrainGenerated");

        Texture2D tex = new Texture2D(16,16,TextureFormat.RGBA32,false);
        Color[] pix = new Color[16*16]; for(int i=0;i<pix.Length;i++) pix[i]=color;
        tex.SetPixels(pix); tex.Apply();

        string texPath = Path.Combine(generatedFolder,name+"_tex.png").Replace("\\","/");
        texPath = AssetDatabase.GenerateUniqueAssetPath(texPath);
        File.WriteAllBytes(texPath,tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);
        Texture2D importedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        TerrainLayer layer = new TerrainLayer();
        layer.diffuseTexture = importedTex;
        layer.name=name;

        string layerPath = Path.Combine(generatedFolder,name+".terrainlayer").Replace("\\","/");
        layerPath = AssetDatabase.GenerateUniqueAssetPath(layerPath);
        AssetDatabase.CreateAsset(layer,layerPath);
        AssetDatabase.SaveAssets();

        return AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
    }
#else
    TerrainLayer MakeAndSaveTerrainLayer(string name, Color color){ TerrainLayer l=new TerrainLayer(); Texture2D tex=new Texture2D(1,1); tex.SetPixel(0,0,color); tex.Apply(); l.diffuseTexture=tex; return l; }
#endif
}

// Editor Window
    public class ProceduralTerrainGeneratorWindow : EditorWindow
    {
        ProceduralTerrainGeneratorV2 targetComp;
        Texture2D previewTex;

        [MenuItem("Window/Procedural Terrain Generator V2")]
        public static void ShowWindow() { GetWindow<ProceduralTerrainGeneratorWindow>("ProcTerrainV2"); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Procedural Terrain Generator V2", EditorStyles.boldLabel);
            targetComp = EditorGUILayout.ObjectField("Target Component", targetComp, typeof(ProceduralTerrainGeneratorV2), true) as ProceduralTerrainGeneratorV2;
            if (targetComp == null)
            {
                EditorGUILayout.HelpBox("Create a GameObject and add ProceduralTerrainGeneratorV2 component, then assign it here.", MessageType.Info);
                if (GUILayout.Button("Create new Component")) { var go = new GameObject("ProceduralTerrainGeneratorV2"); targetComp = go.AddComponent<ProceduralTerrainGeneratorV2>(); Selection.activeGameObject = go; }
                return;
            }

            SerializedObject so = new SerializedObject(targetComp); so.Update();
            EditorGUILayout.PropertyField(so.FindProperty("baseGridSize")); 
            EditorGUILayout.PropertyField(so.FindProperty("aspect")); 
            EditorGUILayout.PropertyField(so.FindProperty("seed"));
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Road", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("startEdge")); EditorGUILayout.PropertyField(so.FindProperty("startEdgePos01")); EditorGUILayout.PropertyField(so.FindProperty("endEdge")); EditorGUILayout.PropertyField(so.FindProperty("endEdgePos01"));
            EditorGUILayout.PropertyField(so.FindProperty("roadColor")); EditorGUILayout.PropertyField(so.FindProperty("roadWidthCells")); EditorGUILayout.PropertyField(so.FindProperty("sinuosity")); EditorGUILayout.PropertyField(so.FindProperty("passEveryN"));
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Plateaus", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("plateaus"), true);
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Preview")) { targetComp.GenerateMapMask(); RebuildPreview(); }
            EditorGUILayout.BeginHorizontal(); if (GUILayout.Button("Generate Terrain (create)")) { targetComp.GenerateMapMask(); targetComp.GenerateTerrainFromMask(true); } if (GUILayout.Button("Generate Terrain (replace)")) { targetComp.GenerateTerrainFromMask(true); } EditorGUILayout.EndHorizontal();

            if (previewTex != null) GUILayout.Label(previewTex, GUILayout.Width(512), GUILayout.Height(512 * ((float)targetComp.mapHeight / Mathf.Max(1, targetComp.mapWidth)))); else EditorGUILayout.HelpBox("No preview — click Generate Preview.", MessageType.Info);

            so.ApplyModifiedProperties();
        }

        void RebuildPreview()
        {
            if (targetComp == null) return; if (targetComp.mapMask == null) targetComp.GenerateMapMask(); int w = targetComp.mapWidth, h = targetComp.mapHeight; int pxW = Mathf.Clamp(w * targetComp.previewPixelPerCell, 64, 2048); int pxH = Mathf.Clamp(h * targetComp.previewPixelPerCell, 64, 2048);
            previewTex = new Texture2D(pxW, pxH, TextureFormat.RGBA32, false);
            Color baseC = Color.Lerp(Color.green, Color.gray, 0.2f);
            for (int y = 0; y < pxH; y++) for (int x = 0; x < pxW; x++) previewTex.SetPixel(x, y, baseC);
            for (int gy = 0; gy < h; gy++) for (int gx = 0; gx < w; gx++)
            {
                int v = targetComp.mapMask[gx, gy]; Color c = baseC;
                if (v == 1) c = targetComp.roadColor; else if (v >= 2) { int id = v - 2; if (id >= 0 && id < targetComp.plateaus.Count) c = targetComp.plateaus[id].color; else c = Color.white; }
                int cx = Mathf.RoundToInt(gx * (pxW - 1) / (float)Math.Max(1, w - 1)); int cy = Mathf.RoundToInt(gy * (pxH - 1) / (float)Math.Max(1, h - 1));
                int tile = targetComp.previewPixelPerCell; for (int yy = cy; yy < cy + tile && yy < pxH; yy++) for (int xx = cx; xx < cx + tile && xx < pxW; xx++) previewTex.SetPixel(xx, yy, c);
            }
            previewTex.Apply(); Repaint();
        }
    }