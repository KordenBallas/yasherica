using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
Updated Procedural Terrain Implementation
- RoadMask is binary (0/1)
- PlatoMask is 0..N (each plato has its id)
- Road rasterization: Bresenham + FillCircle for width
- Plato placement: FillCircle, flattening heights inside plato
- Terrain painting uses TerrainLayer colors created from SO colors (1x1 textures)
- EditorWindow shows separate preview layers: Height / Road / Plato / Combined
- Export maps: height, road, plato

This file is a single-file prototype. For production split to multiple files.
*/

namespace ProceduralTerrain
{
    #region ScriptableObjects

    [Serializable]
    public struct DecorationParams
    {
        public float treeDensity;
        public float rockDensity;
        public float minDistanceBetweenObjects;
        public float slopeTolerance;
        public GameObject[] treePrefabs;
        public GameObject[] rockPrefabs;
    }

    [CreateAssetMenu(menuName = "PT/LandscapeParamsSO")]
    public class LandscapeParamsSO : ScriptableObject
    {
        public int seed = 12345;
        public int mapSize = 512;
        public float noiseScale = 0.01f;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2.0f;
        public Vector2 heightRange = new Vector2(0f, 50f);
        public bool useIslandMask = false;
        public int erosionIterations = 0;
        public Gradient colorMap;
        public Texture2D colorsMap;
        public DecorationParams decorationParams;
    }

    public enum Edge { Left, Right, Top, Bottom }

    [CreateAssetMenu(menuName = "PT/RoadParamsSO")]
    public class RoadParamsSO : ScriptableObject
    {
        public Edge startEdge = Edge.Left;
        public Edge endEdge = Edge.Right;
        public Vector2 endRange01 = new Vector2(0.25f, 0.75f);
        public float roadWidthMeters = 4f;
        public float maxSlope = 20f; // degrees
        public Color roadColor = Color.gray;
        public float curvatureInfluence = 0.5f;
        public float branchProbability = 0.12f;
        public float branchDecay = 0.6f;
        public float branchLengthMean = 60f;
        public float branchLengthStd = 20f;
        public int maxBranchDepth = 3;
        public bool ensureConnectivity = true;
    }

    public enum PlatoType { Camp, Village, Shrine }

    [CreateAssetMenu(menuName = "PT/PlatoParamsSO")]
    public class PlatoParamsSO : ScriptableObject
    {
        public string platoName = "Plato";
        public float minRadiusMeters = 8f;
        public float maxRadiusMeters = 16f;
        public float clearanceMeters = 6f;
        public float requiredIsolationScore = 1f;
        public Color platoColor = Color.green;
        public GameObject[] spawnPrefabs;
        public float spawnDensity = 0.2f;
        public PlatoType platoType = PlatoType.Camp;
    }

    #endregion

    #region MapData and Graphs

    [Serializable]
    public class MapData
    {
        public int size;
        public float[,] HeightMap;
        public float[,] SlopeMap;
        public float[,] CostMap;
        public int[,] RoadMask;    // 0 or 1
        public int[,] PlatoMask;   // 0 or platoId
        public int[,] BlockerMask;
        public int[,] DecorationMask;
        public RoadGraph RoadGraph = new RoadGraph();

        public MapData(int size)
        {
            this.size = size;
            HeightMap = new float[size, size];
            SlopeMap = new float[size, size];
            CostMap = new float[size, size];
            RoadMask = new int[size, size];
            PlatoMask = new int[size, size];
            BlockerMask = new int[size, size];
            DecorationMask = new int[size, size];
        }

        public float GetHeight(int x, int y) => HeightMap[x, y];
        public void SetHeight(int x, int y, float h) => HeightMap[x, y] = h;
        public bool IsRoad(int x, int y) => RoadMask[x, y] != 0;
        public bool IsPlato(int x, int y) => PlatoMask[x, y] != 0;

        public Texture2D ToTextureHeightmap()
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float maxH = float.MinValue, minH = float.MaxValue;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { maxH = Math.Max(maxH, HeightMap[x, y]); minH = Math.Min(minH, HeightMap[x, y]); }
            float range = Mathf.Max(1e-5f, maxH - minH);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = (HeightMap[x, y] - minH) / range;
                tex.SetPixel(x, y, new Color(n, n, n));
            }
            tex.Apply();
            return tex;
        }

        public Texture2D ToTextureRoadMask()
        {
            Texture2D tex = new Texture2D(size, size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++) tex.SetPixel(x, y, RoadMask[x, y] != 0 ? Color.red : Color.black);
            tex.Apply();
            return tex;
        }

        public Texture2D ToTexturePlatoMask()
        {
            Texture2D tex = new Texture2D(size, size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++) tex.SetPixel(x, y, PlatoMask[x, y] != 0 ? Color.green : Color.black);
            tex.Apply();
            return tex;
        }

        public Texture2D ToCombinedDebugTexture(Color roadColor, Color platoColor)
        {
            Texture2D tex = new Texture2D(size, size);
            float maxH = float.MinValue, minH = float.MaxValue;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { maxH = Math.Max(maxH, HeightMap[x, y]); minH = Math.Min(minH, HeightMap[x, y]); }
            float range = Mathf.Max(1e-5f, maxH - minH);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = (HeightMap[x, y] - minH) / range;
                Color c = new Color(n, n, n);
                if (PlatoMask[x, y] != 0) c = Color.Lerp(c, platoColor, 0.85f);
                else if (RoadMask[x, y] != 0) c = Color.Lerp(c, roadColor, 0.9f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }
    }

    [Serializable]
    public class RoadNode { public int id; public Vector2Int position; }
    [Serializable]
    public class RoadEdge { public int a, b; public float cost; }

    [Serializable]
    public class RoadGraph
    {
        public List<RoadNode> nodes = new List<RoadNode>();
        public List<RoadEdge> edges = new List<RoadEdge>();

        public int AddNode(Vector2Int pos)
        {
            var node = new RoadNode { id = nodes.Count, position = pos };
            nodes.Add(node);
            return node.id;
        }

        public void AddEdge(int a, int b, float cost)
        {
            edges.Add(new RoadEdge { a = a, b = b, cost = cost });
        }

        public RoadNode GetNearestNode(Vector2Int pos)
        {
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                float d = Vector2Int.Distance(nodes[i].position, pos);
                if (d < bd) { bd = d; best = i; }
            }
            return best >= 0 ? nodes[best] : null;
        }

        public bool PathExists(int nodeA, int nodeB) => GetPath(nodeA, nodeB) != null;

        public List<int> GetPath(int nodeA, int nodeB)
        {
            int n = nodes.Count;
            if (nodeA < 0 || nodeB < 0 || nodeA >= n || nodeB >= n) return null;
            float[] dist = new float[n]; int[] prev = new int[n]; bool[] used = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; prev[i] = -1; used[i] = false; }
            dist[nodeA] = 0f;
            for (int iter = 0; iter < n; iter++)
            {
                int v = -1; float bd = float.MaxValue;
                for (int i = 0; i < n; i++) if (!used[i] && dist[i] < bd) { bd = dist[i]; v = i; }
                if (v == -1) break;
                used[v] = true;
                foreach (var e in edges)
                {
                    int to = -1; float w = 0f;
                    if (e.a == v) { to = e.b; w = e.cost; }
                    else if (e.b == v) { to = e.a; w = e.cost; }
                    if (to >= 0 && dist[v] + w < dist[to]) { dist[to] = dist[v] + w; prev[to] = v; }
                }
            }
            if (dist[nodeB] == float.MaxValue) return null;
            List<int> path = new List<int>(); int cur = nodeB;
            while (cur != -1) { path.Add(cur); cur = prev[cur]; }
            path.Reverse(); return path;
        }
    }

    [Serializable]
    public class PlatoInfo
    {
        public int platoId;
        public Vector2Int center;
        public float radius;
        public PlatoParamsSO config;
        public RectInt bounds;
    }

    #endregion

    #region Modules

    public interface IGeneratorModule
    {
        void Initialize(MapData map, ScriptableObject config);
        void Generate();
        void Reset();
        void DebugDrawGizmos();
    }

    public class LandscapeModule : IGeneratorModule
    {
        private MapData map;
        private LandscapeParamsSO config;

        public void Initialize(MapData map, ScriptableObject cfg)
        {
            this.map = map; this.config = cfg as LandscapeParamsSO;
        }

        public void Generate()
        {
            int size = map.size;
            UnityEngine.Random.InitState(config.seed);
            float scale = config.noiseScale;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float nx = x * scale; float ny = y * scale;
                float v = Fbm(nx, ny, config.octaves, config.persistence, config.lacunarity);
                map.HeightMap[x, y] = Mathf.Lerp(config.heightRange.x, config.heightRange.y, v);
            }
            if (config.useIslandMask) ApplyIslandMask();
            if (config.erosionIterations > 0) ApplySimpleErosion(config.erosionIterations);
            GenerateSlopeMap();
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) map.CostMap[x, y] = 1f + map.SlopeMap[x, y];
        }

        private void ApplyIslandMask()
        {
            int size = map.size; Vector2 center = new Vector2(size / 2f, size / 2f); float maxDist = size / 2f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { float d = Vector2.Distance(new Vector2(x, y), center) / maxDist; float mask = Mathf.Clamp01(1f - d * d); map.HeightMap[x, y] *= mask; }
        }

        private void ApplySimpleErosion(int iterations)
        {
            int size = map.size;
            for (int it = 0; it < iterations; it++)
            {
                float[,] tmp = new float[size, size];
                for (int y = 1; y < size - 1; y++) for (int x = 1; x < size - 1; x++) { float sum = 0f; int c = 0; for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++) { sum += map.HeightMap[x + ox, y + oy]; c++; } tmp[x, y] = sum / c; }
                for (int y = 1; y < size - 1; y++) for (int x = 1; x < size - 1; x++) map.HeightMap[x, y] = tmp[x, y];
            }
        }

        private void GenerateSlopeMap()
        {
            int size = map.size;
            for (int y = 1; y < size - 1; y++) for (int x = 1; x < size - 1; x++) { float dhdx = (map.HeightMap[x + 1, y] - map.HeightMap[x - 1, y]) * 0.5f; float dhdy = (map.HeightMap[x, y + 1] - map.HeightMap[x, y - 1]) * 0.5f; float slope = Mathf.Sqrt(dhdx * dhdx + dhdy * dhdy); map.SlopeMap[x, y] = slope; }
        }

        private float Fbm(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float total = 0f; float amplitude = 1f; float frequency = 1f; float maxValue = 0f;
            for (int i = 0; i < octaves; i++) { float v = Mathf.PerlinNoise(x * frequency, y * frequency); total += v * amplitude; maxValue += amplitude; amplitude *= persistence; frequency *= lacunarity; }
            return total / maxValue;
        }

        public void Reset() { }
        public void DebugDrawGizmos() { }
    }

    public class RoadModule : IGeneratorModule
    {
        private MapData map; private RoadParamsSO config; private List<Vector2Int> platoCandidates = new List<Vector2Int>();

        public void Initialize(MapData map, ScriptableObject cfg) { this.map = map; this.config = cfg as RoadParamsSO; }

        public void Generate()
        {
            platoCandidates.Clear(); int size = map.size;
            // clear masks
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) map.RoadMask[x, y] = 0;

            Vector2Int start = ChoosePointOnEdge(config.startEdge, UnityEngine.Random.Range(0, size));
            int endParam = Mathf.RoundToInt(UnityEngine.Random.Range(config.endRange01.x * size, config.endRange01.y * size));
            Vector2Int end = ChoosePointOnEdge(config.endEdge, endParam);

            List<Vector2Int> control = BuildControlPoints(start, end);
            List<Vector2Int> path = StitchPath(control);
            // rasterize path continuous
            RasterizePathAsRoad(path, Mathf.CeilToInt(config.roadWidthMeters));

            // nodes for graph
            map.RoadGraph = new RoadGraph();
            int prevNode = -1; int step = Mathf.Max(1, path.Count / 30);
            for (int i = 0; i < path.Count; i += step)
            {
                int nid = map.RoadGraph.AddNode(path[i]);
                if (prevNode != -1) map.RoadGraph.AddEdge(prevNode, nid, Vector2Int.Distance(map.RoadGraph.nodes[prevNode].position, map.RoadGraph.nodes[nid].position));
                prevNode = nid;
            }

            // generate branches
            GenerateBranches(path, 0, config.branchProbability, 0);
        }

        private Vector2Int ChoosePointOnEdge(Edge e, int param)
        {
            int size = map.size; param = Mathf.Clamp(param, 0, size - 1);
            switch (e)
            {
                case Edge.Left: return new Vector2Int(0, param);
                case Edge.Right: return new Vector2Int(size - 1, param);
                case Edge.Top: return new Vector2Int(param, size - 1);
                case Edge.Bottom: return new Vector2Int(param, 0);
                default: return Vector2Int.zero;
            }
        }

        private List<Vector2Int> BuildControlPoints(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> cp = new List<Vector2Int> { start };
            int size = map.size; int midCount = 3;
            for (int i = 0; i < midCount; i++)
            {
                float t = (i + 1f) / (midCount + 1f);
                int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
                float nx = x * config.curvatureInfluence * 0.001f; float ny = y * config.curvatureInfluence * 0.001f;
                x += Mathf.RoundToInt((Mathf.PerlinNoise(nx + config.curvatureInfluence, ny) - 0.5f) * size * 0.08f);
                y += Mathf.RoundToInt((Mathf.PerlinNoise(nx, ny + config.curvatureInfluence) - 0.5f) * size * 0.08f);
                cp.Add(new Vector2Int(Mathf.Clamp(x, 0, size - 1), Mathf.Clamp(y, 0, size - 1)));
            }
            cp.Add(end);
            return cp;
        }

        private List<Vector2Int> StitchPath(List<Vector2Int> control)
        {
            List<Vector2Int> outp = new List<Vector2Int>();
            for (int i = 0; i < control.Count - 1; i++) foreach (var p in BresenhamLine(control[i].x, control[i].y, control[i + 1].x, control[i + 1].y)) outp.Add(p);
            // try A* refine between segments to avoid steep slopes
            var refined = AStarPath(outp[0], outp[outp.Count - 1]);
            return refined ?? outp;
        }

        private IEnumerable<Vector2Int> BresenhamLine(int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, e2;
            while (true)
            {
                yield return new Vector2Int(x0, y0);
                if (x0 == x1 && y0 == y1) yield break;
                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private List<Vector2Int> AStarPath(Vector2Int start, Vector2Int goal)
        {
            int size = map.size;
            bool[,] closed = new bool[size, size];
            float[,] gscore = new float[size, size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) gscore[x, y] = float.MaxValue;
            PriorityQueue<Vector2Int> open = new PriorityQueue<Vector2Int>();
            gscore[start.x, start.y] = 0f; open.Enqueue(start, Heuristic(start, goal));
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            int[,] dir8 = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 }, { 1, 1 } };
            while (open.Count > 0)
            {
                var current = open.Dequeue();
                if (current == goal) break;
                closed[current.x, current.y] = true;
                for (int i = 0; i < 8; i++)
                {
                    int nx = current.x + dir8[i, 0]; int ny = current.y + dir8[i, 1];
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size) continue;
                    if (closed[nx, ny]) continue;
                    float slope = map.SlopeMap[nx, ny];
                    if (slope > Mathf.Tan(config.maxSlope * Mathf.Deg2Rad)) continue;
                    float tentativeG = gscore[current.x, current.y] + map.CostMap[nx, ny];
                    if (tentativeG < gscore[nx, ny]) { cameFrom[new Vector2Int(nx, ny)] = current; gscore[nx, ny] = tentativeG; float f = tentativeG + Heuristic(new Vector2Int(nx, ny), goal); open.Enqueue(new Vector2Int(nx, ny), f); }
                }
            }
            if (!cameFrom.ContainsKey(goal)) return null;
            List<Vector2Int> path = new List<Vector2Int>(); Vector2Int cur = goal; path.Add(cur);
            while (!cur.Equals(start)) { if (!cameFrom.TryGetValue(cur, out cur)) break; path.Add(cur); }
            path.Reverse(); return path;
        }

        private float Heuristic(Vector2Int a, Vector2Int b) => Vector2Int.Distance(a, b);

        private void RasterizePathAsRoad(List<Vector2Int> path, int width)
        {
            int size = map.size; int r = Mathf.Max(1, width);
            for (int i = 0; i < path.Count - 1; i++)
            {
                foreach (var p in BresenhamLine(path[i].x, path[i].y, path[i + 1].x, path[i + 1].y)) FillCircleOnMask(p.x, p.y, r);
            }
        }

        private void FillCircleOnMask(int cx, int cy, int r)
        {
            int size = map.size; int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++) for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= size || y >= size) continue;
                int dx = x - cx, dy = y - cy; if (dx * dx + dy * dy <= r2) map.RoadMask[x, y] = 1;
            }
        }

        private void GenerateBranches(List<Vector2Int> basePath, int depth, float probability, int recursion)
        {
            if (recursion > config.maxBranchDepth) return;
            for (int i = 0; i < basePath.Count; i++)
            {
                if (UnityEngine.Random.value > probability) continue;
                var origin = basePath[i];
                Vector2Int dir = new Vector2Int(UnityEngine.Random.Range(-1, 2), UnityEngine.Random.Range(-1, 2)); if (dir == Vector2Int.zero) dir = Vector2Int.up;
                int len = Mathf.Clamp(Mathf.RoundToInt(UnityEngine.Random.Range(config.branchLengthMean - config.branchLengthStd, config.branchLengthMean + config.branchLengthStd)), 5, map.size / 4);
                List<Vector2Int> branch = new List<Vector2Int>(); Vector2Int cur = origin;
                for (int s = 0; s < len; s++) { cur += dir; cur.x = Mathf.Clamp(cur.x, 0, map.size - 1); cur.y = Mathf.Clamp(cur.y, 0, map.size - 1); branch.Add(cur); if (UnityEngine.Random.value < 0.12f) dir = new Vector2Int(UnityEngine.Random.Range(-1, 2), UnityEngine.Random.Range(-1, 2)); }
                RasterizePathAsRoad(branch, Mathf.CeilToInt(config.roadWidthMeters)); if (branch.Count > 0) platoCandidates.Add(branch[branch.Count - 1]);
                GenerateBranches(branch, depth + 1, probability * config.branchDecay, recursion + 1);
            }
        }

        public List<Vector2Int> GetPlatoCandidates() => platoCandidates;

        public void Reset() { map.RoadGraph = new RoadGraph(); for (int y = 0; y < map.size; y++) for (int x = 0; x < map.size; x++) map.RoadMask[x, y] = 0; }
        public void DebugDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.red; foreach (var n in map.RoadGraph.nodes) Gizmos.DrawSphere(new Vector3(n.position.x, 0, n.position.y), 0.5f);
            Gizmos.color = Color.yellow; foreach (var e in map.RoadGraph.edges) { var a = map.RoadGraph.nodes[e.a]; var b = map.RoadGraph.nodes[e.b]; Gizmos.DrawLine(new Vector3(a.position.x, 0.1f, a.position.y), new Vector3(b.position.x, 0.1f, b.position.y)); }
#endif
        }
    }

    public class PlatoModule : IGeneratorModule
    {
        private MapData map; private PlatoParamsSO[] configs; private List<PlatoInfo> platos = new List<PlatoInfo>();

        public void Initialize(MapData map, ScriptableObject cfg) { this.map = map; /*this.configs = cfg as PlatoParamsSO[] ?? new PlatoParamsSO[0];*/ }

        public void Initialize(MapData map, ScriptableObject[] cfg)
        {
            this.map = map; 
            this.configs = cfg as PlatoParamsSO[] ?? new PlatoParamsSO[0];
            Debug.Log("PlatoModule initialized with " + this.configs.Length + " configs.");
        }

        public void Generate()
        {
            platos.Clear(); for (int y = 0; y < map.size; y++) for (int x = 0; x < map.size; x++) map.PlatoMask[x, y] = 0;
            // find dead-ends in road mask
            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int y = 1; y < map.size - 1; y++) for (int x = 1; x < map.size - 1; x++) if (map.RoadMask[x, y] != 0)
            {
                int neigh = 0; for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++) if (!(ox == 0 && oy == 0)) { int nx = x + ox, ny = y + oy; if (nx < 0 || ny < 0 || nx >= map.size || ny >= map.size) continue; if (map.RoadMask[nx, ny] != 0) neigh++; }
                if (neigh <= 1) candidates.Add(new Vector2Int(x, y));
            }

            Debug.Log("PlatoModule found " + candidates.Count + " plato candidates.");
            int platoId = 1; foreach (var cand in candidates)
            {
                if (configs.Length == 0) break; var conf = configs[UnityEngine.Random.Range(0, configs.Length)]; float radius = UnityEngine.Random.Range(conf.minRadiusMeters, conf.maxRadiusMeters);
                if (TryPlacePlato(platoId, cand, radius, conf)) platoId++;
            }
        }

        private bool TryPlacePlato(int id, Vector2Int center, float radius, PlatoParamsSO conf)
        {
            int r = Mathf.CeilToInt(radius);
            RectInt bounds = new RectInt(center.x - r, center.y - r, r * 2 + 1, r * 2 + 1);
            if (bounds.xMin < 0 || bounds.yMin < 0 || bounds.xMax >= map.size || bounds.yMax >= map.size) return false;
            // collision with existing plato
            for (int y = bounds.yMin; y <= bounds.yMax; y++) for (int x = bounds.xMin; x <= bounds.xMax; x++) if (map.PlatoMask[x, y] != 0) return false;
            // slope check inside
            for (int y = bounds.yMin; y <= bounds.yMax; y++) for (int x = bounds.xMin; x <= bounds.xMax; x++) if (Vector2Int.Distance(new Vector2Int(x, y), center) <= r) if (map.SlopeMap[x, y] > 0.6f) return false;
            // place plato id
            for (int y = bounds.yMin; y <= bounds.yMax; y++) for (int x = bounds.xMin; x <= bounds.xMax; x++) if (Vector2Int.Distance(new Vector2Int(x, y), center) <= r) map.PlatoMask[x, y] = id;
            // flatten heights inside
            float target = AverageHeightInCircle(center, r);
            FeatherAndFlatten(center, r, target);
            // create blocker ring
            CreateBlockerRing(center, r, Mathf.CeilToInt(conf.clearanceMeters));
            PlatoInfo pi = new PlatoInfo { platoId = id, center = center, radius = radius, config = conf, bounds = bounds };
            platos.Add(pi);
            Debug.Log("Placed plato id " + id + " at " + center + " with radius " + radius);
            return true;
        }

        private float AverageHeightInCircle(Vector2Int center, int r)
        {
            float sum = 0f; int c = 0; for (int y = center.y - r; y <= center.y + r; y++) for (int x = center.x - r; x <= center.x + r; x++) if (x >= 0 && y >= 0 && x < map.size && y < map.size) if (Vector2Int.Distance(new Vector2Int(x, y), center) <= r) { sum += map.HeightMap[x, y]; c++; }
            return c == 0 ? 0f : sum / c;
        }

        private void FeatherAndFlatten(Vector2Int center, int r, float target)
        {
            int size = map.size; int outer = r + 4;
            for (int y = center.y - outer; y <= center.y + outer; y++) for (int x = center.x - outer; x <= center.x + outer; x++)
            {
                if (x < 0 || y < 0 || x >= size || y >= size) continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));
                if (d <= r) map.HeightMap[x, y] = Mathf.Lerp(map.HeightMap[x, y], target, 0.92f);
                else if (d <= outer) { float t = (d - r) / (outer - r); map.HeightMap[x, y] = Mathf.Lerp(target, map.HeightMap[x, y], t); }
            }
        }

        private void CreateBlockerRing(Vector2Int center, int r, int width)
        {
            int size = map.size; int inner = r + 1; int outer = r + width;
            for (int y = center.y - outer; y <= center.y + outer; y++) for (int x = center.x - outer; x <= center.x + outer; x++)
            {
                if (x < 0 || y < 0 || x >= size || y >= size) continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));
                if (d >= inner && d <= outer) { map.BlockerMask[x, y] = 1; map.HeightMap[x, y] += UnityEngine.Random.Range(1f, 3f); }
            }
        }

        public List<PlatoInfo> GetPlatos() => platos;
        public void Reset() { for (int y = 0; y < map.size; y++) for (int x = 0; x < map.size; x++) { map.PlatoMask[x, y] = 0; map.BlockerMask[x, y] = 0; } platos.Clear(); }
        public void DebugDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.green; foreach (var p in platos) Gizmos.DrawWireSphere(new Vector3(p.center.x, 0, p.center.y), p.radius);
#endif
        }
    }

    public class DecorationModule : IGeneratorModule
    {
        private MapData map; private LandscapeParamsSO config;
        public void Initialize(MapData map, ScriptableObject cfg) { this.map = map; this.config = cfg as LandscapeParamsSO; }
        public void Generate()
        {
            int size = map.size; float treeDensity = config.decorationParams.treeDensity; float rockDensity = config.decorationParams.rockDensity;
            for (int y = 1; y < size - 1; y++) for (int x = 1; x < size - 1; x++)
            {
                if (map.RoadMask[x, y] != 0) continue; if (map.PlatoMask[x, y] != 0) continue; if (map.BlockerMask[x, y] != 0) { map.DecorationMask[x, y] = 2; continue; }
                if (map.SlopeMap[x, y] > config.decorationParams.slopeTolerance) continue;
                if (UnityEngine.Random.value < treeDensity) map.DecorationMask[x, y] = 1; else if (UnityEngine.Random.value < rockDensity) map.DecorationMask[x, y] = 3;
            }
        }
        public void Reset() { for (int y = 0; y < map.size; y++) for (int x = 0; x < map.size; x++) map.DecorationMask[x, y] = 0; }
        public void DebugDrawGizmos() { }
    }

    public class MeshComposerModule
    {
        private MapData map; private LandscapeParamsSO landscapeConfig; private RoadParamsSO roadConfig; private PlatoParamsSO[] platoConfigs;
        public void Initialize(MapData map, LandscapeParamsSO l = null, RoadParamsSO r = null, PlatoParamsSO[] p = null) { this.map = map; this.landscapeConfig = l; this.roadConfig = r; this.platoConfigs = p; }

        public Terrain BuildTerrain(GameObject parent = null)
        {
            int size = map.size; TerrainData td = new TerrainData();
            int res = Mathf.NextPowerOfTwo(size) + 1; td.heightmapResolution = Mathf.Clamp(res, 33, 4097);
            td.size = new Vector3(size, Mathf.Max(50f, landscapeConfig.heightRange.y - landscapeConfig.heightRange.x), size);
            float[,] heights = new float[td.heightmapResolution, td.heightmapResolution];
            for (int y = 0; y < td.heightmapResolution; y++) for (int x = 0; x < td.heightmapResolution; x++)
            {
                float u = (float)x / (td.heightmapResolution - 1); float v = (float)y / (td.heightmapResolution - 1);
                int sx = Mathf.Clamp(Mathf.RoundToInt(u * (size - 1)), 0, size - 1); int sy = Mathf.Clamp(Mathf.RoundToInt(v * (size - 1)), 0, size - 1);
                heights[y, x] = (map.HeightMap[sx, sy] - landscapeConfig.heightRange.x) / td.size.y;
            }
            td.SetHeights(0, 0, heights);

            // create Terrain GameObject
            GameObject terrObj = new GameObject("ProceduralTerrain"); if (parent != null) terrObj.transform.parent = parent.transform;
            var t = terrObj.AddComponent<Terrain>(); var tc = terrObj.AddComponent<TerrainCollider>(); t.terrainData = td; tc.terrainData = td;

            // create TerrainLayers from colors (1x1 textures)
            List<TerrainLayer> layers = new List<TerrainLayer>();
            TerrainLayer baseLayer = CreateColorTerrainLayer("Base", landscapeConfig.colorMap != null ? landscapeConfig.colorMap.Evaluate(0.5f) : Color.Lerp(Color.green, Color.gray, 0.5f)); layers.Add(baseLayer);
            TerrainLayer roadLayer = CreateColorTerrainLayer("Road", roadConfig != null ? roadConfig.roadColor : Color.gray); layers.Add(roadLayer);
            // plato layers: one layer for all platos (could be extended per-type)
            Color platoColor = (platoConfigs != null && platoConfigs.Length > 0) ? platoConfigs[0].platoColor : Color.green;
            TerrainLayer platoLayer = CreateColorTerrainLayer("Plato", platoColor); layers.Add(platoLayer);
            td.terrainLayers = layers.ToArray();

            // build alphamap (splatmap) using masks; alphamap resolution
            int alphRes = Mathf.Clamp(256, 32, 2048);
            td.alphamapResolution = alphRes;
            int numLayers = td.terrainLayers.Length;
            float[,,] alphas = new float[alphRes, alphRes, numLayers];
            for (int y = 0; y < alphRes; y++) for (int x = 0; x < alphRes; x++)
            {
                float u = (float)x / (alphRes - 1); float v = (float)y / (alphRes - 1);
                int sx = Mathf.Clamp(Mathf.RoundToInt(u * (size - 1)), 0, size - 1);
                int sy = Mathf.Clamp(Mathf.RoundToInt(v * (size - 1)), 0, size - 1);
                // prioritise plato > road > base
                if (map.PlatoMask[sx, sy] != 0) { alphas[y, x, 2] = 1f; }
                else if (map.RoadMask[sx, sy] != 0) { alphas[y, x, 1] = 1f; }
                else { alphas[y, x, 0] = 1f; }
            }
            td.SetAlphamaps(0, 0, alphas);

            return t;
        }

        private TerrainLayer CreateColorTerrainLayer(string name, Color color)
        {
            TerrainLayer layer = new TerrainLayer(); layer.name = name; Texture2D tex = new Texture2D(1, 1); tex.SetPixel(0, 0, color); tex.Apply(); layer.diffuseTexture = tex; return layer;
        }
    }

    public class TextureComposerModule
    {
        private MapData map; private LandscapeParamsSO landscapeConfig; private RoadParamsSO roadConfig; private PlatoParamsSO[] platoConfigs;
        public void Initialize(MapData map, LandscapeParamsSO l = null, RoadParamsSO r = null, PlatoParamsSO[] p = null) { this.map = map; this.landscapeConfig = l; this.roadConfig = r; this.platoConfigs = p; }
        public Texture2D ComposeHeightTexture() => map.ToTextureHeightmap();
        public Texture2D ComposeRoadTexture() => map.ToTextureRoadMask();
        public Texture2D ComposePlatoTexture() => map.ToTexturePlatoMask();
        public Texture2D ComposeCombinedTexture() { Color rc = roadConfig != null ? roadConfig.roadColor : Color.red; Color pc = (platoConfigs != null && platoConfigs.Length > 0) ? platoConfigs[0].platoColor : Color.green; return map.ToCombinedDebugTexture(rc, pc); }
    }

    #endregion

    #region TerrainBuilder and Generator

    public class GenerationResult { public bool success = true; public string message = "OK"; public List<PlatoInfo> platos = new List<PlatoInfo>(); }

    public class TerrainBuilder
    {
        private LandscapeParamsSO L; private RoadParamsSO R; private PlatoParamsSO[] P; private MapData map;
        private LandscapeModule landscapeModule = new LandscapeModule();
        private RoadModule roadModule = new RoadModule();
        private PlatoModule platoModule = new PlatoModule();
        private DecorationModule decorationModule = new DecorationModule();
        private MeshComposerModule meshModule = new MeshComposerModule();
        private TextureComposerModule textureModule = new TextureComposerModule();

        public void Initialize(LandscapeParamsSO l, RoadParamsSO r, PlatoParamsSO[] p, int seedOverride = -1)
        {
            L = l; R = r; P = p; int size = L.mapSize; map = new MapData(size);
            landscapeModule.Initialize(map, L);
            roadModule.Initialize(map, R);
            platoModule.Initialize(map, P);
            decorationModule.Initialize(map, L);
            meshModule.Initialize(map, L, R, P);
            textureModule.Initialize(map, L, R, P);
        }

        public GenerationResult GenerateAll()
        {
            GenerationResult res = new GenerationResult();
            try
            {
                GenerateLandscape(); GenerateRoads(); GeneratePlatos(); GenerateDecorations(); ComposeTextures(); BuildTerrainGameObject(); res.platos.AddRange(platoModule.GetPlatos());
            }
            catch (Exception ex) { res.success = false; res.message = ex.ToString(); }
            return res;
        }

        public void GenerateLandscape() { landscapeModule.Generate(); }
        public void GenerateRoads() { roadModule.Reset(); roadModule.Initialize(map, R); roadModule.Generate(); }
        public void GeneratePlatos() { platoModule.Reset(); platoModule.Initialize(map, P); platoModule.Generate(); }
        public void GenerateDecorations() { decorationModule.Reset(); decorationModule.Initialize(map, L); decorationModule.Generate(); }
        public void ComposeTextures() { /* textures are composed on demand via textureModule */ }

        public Terrain BuildTerrainGameObject(GameObject parent = null)
        {
            // Build terrain mesh and paint using masks
            return meshModule.BuildTerrain(parent);
        }

        public MapData GetMapData() => map;
        public void SaveMaps(string path)
        {
            var h = map.ToTextureHeightmap(); byte[] hb = h.EncodeToPNG(); System.IO.File.WriteAllBytes(System.IO.Path.Combine(path, "height.png"), hb);
            var r = map.ToTextureRoadMask(); byte[] rb = r.EncodeToPNG(); System.IO.File.WriteAllBytes(System.IO.Path.Combine(path, "road.png"), rb);
            var p = map.ToTexturePlatoMask(); byte[] pb = p.EncodeToPNG(); System.IO.File.WriteAllBytes(System.IO.Path.Combine(path, "plato.png"), pb);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
    }

    [RequireComponent(typeof(Terrain))]
    public class TerrainGenerator : MonoBehaviour
    {
        public LandscapeParamsSO landscapeParams; public RoadParamsSO roadParams; public PlatoParamsSO[] platos;
        private TerrainBuilder builder = new TerrainBuilder(); private GenerationResult lastResult;

        public void GenerateAll()
        {
            builder.Initialize(landscapeParams, roadParams, platos, landscapeParams.seed);
            lastResult = builder.GenerateAll(); Debug.Log("Generation finished: " + lastResult.success + " msg:" + lastResult.message);
        }
        public void RegenerateLandscape() { builder.GenerateLandscape(); }
        public void RegenerateRoads() { builder.GenerateRoads(); }
        public void RegeneratePlatos() { builder.GeneratePlatos(); }
        public void RegenerateDecorations() { builder.GenerateDecorations(); }
        public GenerationResult GetGenerationResult() => lastResult; public MapData GetMapData() => builder.GetMapData(); public void ExportMaps(string path) => builder.SaveMaps(path);
    }

    #endregion

    #region Utilities

    public class PriorityQueue<T> { private List<KeyValuePair<T, float>> data = new List<KeyValuePair<T, float>>(); public int Count => data.Count; public void Enqueue(T item, float priority) { data.Add(new KeyValuePair<T, float>(item, priority)); } public T Dequeue() { int best = 0; for (int i = 1; i < data.Count; i++) if (data[i].Value < data[best].Value) best = i; T val = data[best].Key; data.RemoveAt(best); return val; } }

    #endregion

    #region Editor Tools
#if UNITY_EDITOR
    public class TerrainGeneratorWindow : EditorWindow
    {
        TerrainGenerator generator; Texture2D previewTex; int previewLayer = 0; // 0=combined,1=height,2=road,3=plato
        [MenuItem("Window/Procedural Terrain/Generator Window")]
        public static void ShowWindow() { GetWindow<TerrainGeneratorWindow>("Terrain Generator"); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Procedural Terrain Generator", EditorStyles.boldLabel);
            generator = EditorGUILayout.ObjectField("Generator Object", generator, typeof(TerrainGenerator), true) as TerrainGenerator;
            if (generator == null) { EditorGUILayout.HelpBox("Assign a TerrainGenerator in the scene.", MessageType.Info); return; }

            EditorGUILayout.Space(); generator.landscapeParams = EditorGUILayout.ObjectField("Landscape Params", generator.landscapeParams, typeof(LandscapeParamsSO), false) as LandscapeParamsSO;
            generator.roadParams = EditorGUILayout.ObjectField("Road Params", generator.roadParams, typeof(RoadParamsSO), false) as RoadParamsSO;
            int newCount = EditorGUILayout.IntField("Plato count", generator.platos != null ? generator.platos.Length : 0);
            if (generator.platos == null || generator.platos.Length != newCount) Array.Resize(ref generator.platos, newCount);
            for (int i = 0; i < (generator.platos?.Length ?? 0); i++) generator.platos[i] = EditorGUILayout.ObjectField("Plato " + i, generator.platos[i], typeof(PlatoParamsSO), false) as PlatoParamsSO;

            EditorGUILayout.Space(); if (GUILayout.Button("Generate All")) { generator.GenerateAll(); UpdatePreview(); }
            EditorGUILayout.BeginHorizontal(); if (GUILayout.Button("Regenerate Landscape")) { generator.RegenerateLandscape(); UpdatePreview(); } if (GUILayout.Button("Regenerate Roads")) { generator.RegenerateRoads(); UpdatePreview(); } EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(); if (GUILayout.Button("Regenerate Platos")) { generator.RegeneratePlatos(); UpdatePreview(); } if (GUILayout.Button("Regenerate Decorations")) { generator.RegenerateDecorations(); UpdatePreview(); } EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(); previewLayer = GUILayout.Toolbar(previewLayer, new string[] { "Combined", "Height", "Road", "Plato" });
            if (previewTex != null) GUILayout.Label(previewTex, GUILayout.Width(320), GUILayout.Height(320));

            EditorGUILayout.Space(); if (GUILayout.Button("Export Maps to Project/ProceduralMaps")) { string path = System.IO.Path.Combine(Application.dataPath, "ProceduralMaps"); if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path); generator.ExportMaps(path); EditorUtility.DisplayDialog("Export", "Saved maps to " + path, "OK"); }

            Repaint();
        }

        void UpdatePreview()
        {
            if (generator == null) return; var map = generator.GetMapData(); if (map == null) return;
            switch (previewLayer) { case 0: previewTex = generator.GetMapData().ToCombinedDebugTexture(generator.roadParams != null ? generator.roadParams.roadColor : Color.red, (generator.platos != null && generator.platos.Length > 0) ? generator.platos[0].platoColor : Color.green); break; case 1: previewTex = map.ToTextureHeightmap(); break; case 2: previewTex = map.ToTextureRoadMask(); break; case 3: previewTex = map.ToTexturePlatoMask(); break; }
        }

        void OnInspectorUpdate() { UpdatePreview(); }
    }
#endif
    #endregion
}
