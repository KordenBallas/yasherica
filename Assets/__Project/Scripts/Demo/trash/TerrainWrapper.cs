using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum LandscapeType { Plains, Lake, Mountains, Coast }

[Serializable]
public class NoiseParams {
    public float scale = 100f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int seed = 0;
    public Vector2 offset = Vector2.zero;
    public float heightMultiplier = 20f; // final height in world units
}

[Serializable]
public class PlateauSpec {
    [Range(0f,1f)] public float fractionAlongRoad = 0.5f; // 0..1 along the road
    public float lateralOffset = 0f; // lateral offset from road center in world units (+ right, - left)
    public float radius = 8f; // world units
    public float height = 4f;  // absolute world height
    public AnimationCurve falloff = AnimationCurve.EaseInOut(0,1,1,0);
}

[Serializable]
public class RoadConfig {
    public float roadWidth = 3f; // world units
    public float roadFalloff = 2f; // smoothing edge width
    public int smoothingIterations = 2; // post-smooth passes
}

[Serializable]
public class TerrainConfig {
    public LandscapeType landscapeType = LandscapeType.Plains;
    public int heightmapResolution = 513; // must be 2^n + 1
    public Vector3 worldSize = new Vector3(200, 50, 200); // terrain size in units (x,z) and max height (y)
    public NoiseParams plainsNoise = new NoiseParams() { scale = 150f, octaves = 4, persistence = 0.45f, lacunarity = 2f, heightMultiplier = 6f };
    public NoiseParams mountainNoise = new NoiseParams() { scale = 80f, octaves = 6, persistence = 0.5f, lacunarity = 2f, heightMultiplier = 30f };
    public NoiseParams lakeNoise = new NoiseParams() { scale = 220f, octaves = 2, persistence = 0.3f, lacunarity = 1.8f, heightMultiplier = 2f };
    public NoiseParams coastNoise = new NoiseParams() { scale = 120f, octaves = 3, persistence = 0.4f, lacunarity = 2f, heightMultiplier = 8f };
}

[ExecuteInEditMode]
public class TerrainWrapper : MonoBehaviour {
    [Header("Terrain & Config")]
    public Terrain targetTerrain;
    public TerrainConfig config = new TerrainConfig();

    [Header("Zone map (optional)")]
    public Texture2D zoneMap; // optional per-pixel overrides (not required here)

    [Header("Roads & Exits")]
    [Tooltip("Normalized positions on terrain plane (0..1 in x and z). Order defines path sequence (0->1->2->...).")]
    public Vector2[] exits = new Vector2[] { new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f) };
    public RoadConfig roadConfig = new RoadConfig();

    [Header("Plateaus")]
    public PlateauSpec[] plateaus = new PlateauSpec[0];

    [Header("Runtime Options")]
    public bool autoGenerateOnStart = true;

    // internal heightmap (normalized 0..1)
    private float[,] heights;

    void Start() {
        if (!Application.isPlaying && !autoGenerateOnStart) return;
        if (targetTerrain == null) {
            Debug.LogError("TerrainWrapper: Target Terrain not assigned.");
            return;
        }
        GenerateAll();
    }

    [ContextMenu("Generate All")]
    public void GenerateAll() {
        SetupTerrainData();
        GenerateBaseHeightmap();
        GenerateRoads();
        ApplyPlateaus();
        PostProcessSmoothing();
        ApplyHeightsAndLayersToTerrain();
        Debug.Log("TerrainWrapper: generation complete");
    }

    void SetupTerrainData() {
        TerrainData td = targetTerrain.terrainData;
        if (td.heightmapResolution != config.heightmapResolution || td.size != config.worldSize) {
            td.heightmapResolution = config.heightmapResolution;
            td.size = config.worldSize;
        }
    }

    NoiseParams GetNoiseForType(LandscapeType t) {
        switch (t) {
            case LandscapeType.Mountains: return config.mountainNoise;
            case LandscapeType.Lake: return config.lakeNoise;
            case LandscapeType.Coast: return config.coastNoise;
            default: return config.plainsNoise;
        }
    }

    void GenerateBaseHeightmap() {
        TerrainData td = targetTerrain.terrainData;
        int w = td.heightmapResolution;
        int h = td.heightmapResolution;
        heights = new float[w, h];

        NoiseParams np = GetNoiseForType(config.landscapeType);
        System.Random rng = new System.Random(np.seed);
        float seedOffsetX = rng.Next(0, 10000);
        float seedOffsetY = rng.Next(0, 10000);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                // map to world x,z
                float u = (float)x / (w - 1);
                float v = (float)y / (h - 1);
                float worldX = u * td.size.x;
                float worldZ = v * td.size.z;

                float nx = (worldX + np.offset.x + seedOffsetX) / np.scale;
                float ny = (worldZ + np.offset.y + seedOffsetY) / np.scale;

                float n = FractalPerlin(nx, ny, np);
                // n is 0..1, multiply by heightMultiplier then normalize to terrain max (td.size.y)
                float worldHeight = n * np.heightMultiplier;
                float normalized = Mathf.Clamp01(worldHeight / td.size.y);
                heights[x, y] = normalized;
            }
        }
    }

    float FractalPerlin(float x, float y, NoiseParams p) {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxAmp = 0f;

        for (int i = 0; i < p.octaves; i++) {
            float per = Mathf.PerlinNoise((x * frequency) + p.seed, (y * frequency) + p.seed) * 2f - 1f;
            value += per * amplitude;
            maxAmp += amplitude;
            amplitude *= p.persistence;
            frequency *= p.lacunarity;
        }
        value /= maxAmp;
        return (value + 1f) * 0.5f; // 0..1
    }

    // Create a polyline path from exits (ordered). Exits are normalized [0..1] on terrain plane.
    List<Vector2> MakePathPoints(int samplesPerSegment = 64) {
        List<Vector2> pts = new List<Vector2>();
        if (exits == null || exits.Length == 0) return pts;
        // convert normalized exits into world positions on XZ plane
        TerrainData td = targetTerrain.terrainData;
        List<Vector2> worldExits = new List<Vector2>();
        foreach (var e in exits) worldExits.Add(new Vector2(e.x * td.size.x, e.y * td.size.z));

        // if only one exit, make small center-to-edge line
        if (worldExits.Count == 1) {
            pts.Add(worldExits[0]);
            return pts;
        }

        // for each segment between consecutive exits sample points
        for (int i = 0; i < worldExits.Count - 1; i++) {
            Vector2 a = worldExits[i];
            Vector2 b = worldExits[i + 1];
            for (int s = 0; s < samplesPerSegment; s++) {
                float t = (float)s / (samplesPerSegment - 1);
                Vector2 p = Vector2.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
                pts.Add(p);
            }
        }
        return pts;
    }

    // get world position along path at normalized t (0..1)
    Vector2 GetPointAlongPath(List<Vector2> path, float t) {
        if (path == null || path.Count == 0) return Vector2.zero;
        float total = 0f;
        List<float> segLen = new List<float>();
        for (int i = 0; i < path.Count - 1; i++) {
            float l = Vector2.Distance(path[i], path[i + 1]);
            segLen.Add(l); total += l;
        }
        if (total <= 0f) return path[0];
        float target = Mathf.Clamp01(t) * total;
        float acc = 0f;
        for (int i = 0; i < segLen.Count; i++) {
            if (acc + segLen[i] >= target) {
                float localT = (target - acc) / segLen[i];
                return Vector2.Lerp(path[i], path[i + 1], localT);
            }
            acc += segLen[i];
        }
        return path[path.Count - 1];
    }

    void GenerateRoads() {
        if (exits == null || exits.Length < 2) return; // need at least two to make a road
        TerrainData td = targetTerrain.terrainData;
        int w = td.heightmapResolution;
        int h = td.heightmapResolution;

        List<Vector2> path = MakePathPoints(samplesPerSegment: 128);
        float[,] baseHeights = heights; // already normalized

        // For each height sample, compute distance to path (in world units) and blend
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float u = (float)x / (w - 1);
                float v = (float)y / (h - 1);
                Vector2 worldPos = new Vector2(u * td.size.x, v * td.size.z);

                // compute shortest distance to polyline
                float minDist = float.MaxValue;
                Vector2 closestPoint = Vector2.zero;
                for (int i = 0; i < path.Count - 1; i++) {
                    Vector2 a = path[i];
                    Vector2 b = path[i + 1];
                    Vector2 proj = ClosestPointOnSegment(a, b, worldPos);
                    float d = Vector2.Distance(worldPos, proj);
                    if (d < minDist) { minDist = d; closestPoint = proj; }
                }

                float mask = Mathf.Clamp01((roadConfig.roadWidth + roadConfig.roadFalloff - minDist) / (roadConfig.roadWidth + roadConfig.roadFalloff));
                // smoother falloff using quadratic
                mask = Mathf.SmoothStep(0f, 1f, mask);

                if (mask > 0f) {
                    // compute target road height: sample existing heights around closestPoint and flatten
                    Vector2 normalizedClosest = new Vector2(closestPoint.x / td.size.x, closestPoint.y / td.size.z);
                    int cx = Mathf.RoundToInt(normalizedClosest.x * (w - 1));
                    int cy = Mathf.RoundToInt(normalizedClosest.y * (h - 1));
                    float avg = SampleAverageHeight(baseHeights, cx, cy, kernel: Mathf.CeilToInt(roadConfig.roadWidth));

                    // Blend the current cell towards the road average to make it traversable
                    float current = heights[x, y];
                    float blended = Mathf.Lerp(current, avg, mask);

                    // additionally flatten steep slopes by clamping local difference
                    heights[x, y] = blended;
                }
            }
        }

        // optional smoothing passes to remove artifacts
        for (int i = 0; i < roadConfig.smoothingIterations; i++) SmoothHeights(2);
    }

    static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p) {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    float SampleAverageHeight(float[,] arr, int cx, int cy, int kernel = 2) {
        int w = arr.GetLength(0);
        int h = arr.GetLength(1);
        int r = Mathf.Max(1, kernel);
        int xmin = Mathf.Clamp(cx - r, 0, w - 1);
        int xmax = Mathf.Clamp(cx + r, 0, w - 1);
        int ymin = Mathf.Clamp(cy - r, 0, h - 1);
        int ymax = Mathf.Clamp(cy + r, 0, h - 1);
        float sum = 0f; int count = 0;
        for (int y = ymin; y <= ymax; y++) for (int x = xmin; x <= xmax; x++) { sum += arr[x, y]; count++; }
        return count > 0 ? sum / count : arr[cx, cy];
    }

    void ApplyPlateaus() {
        if (plateaus == null || plateaus.Length == 0) return;
        TerrainData td = targetTerrain.terrainData;
        List<Vector2> path = MakePathPoints(samplesPerSegment: 256);

        foreach (var p in plateaus) {
            // determine center on path + lateral offset
            Vector2 center = GetPointAlongPath(path, p.fractionAlongRoad);
            // compute direction along path at that point to get perpendicular
            Vector2 forward = EstimateTangent(path, p.fractionAlongRoad);
            Vector2 perp = new Vector2(-forward.y, forward.x).normalized;
            Vector2 worldCenter = center + perp * p.lateralOffset;

            // stamp plateau into heights
            StampPlateau(worldCenter, p.radius, p.height, p.falloff);
        }
    }

    Vector2 EstimateTangent(List<Vector2> path, float t) {
        // small delta along the path to approximate derivative
        float dt = 0.001f;
        Vector2 a = GetPointAlongPath(path, Mathf.Clamp01(t - dt));
        Vector2 b = GetPointAlongPath(path, Mathf.Clamp01(t + dt));
        Vector2 d = (b - a).normalized;
        if (d == Vector2.zero && path.Count > 1) d = (path[path.Count - 1] - path[0]).normalized;
        return d;
    }

    void StampPlateau(Vector2 worldCenter, float radius, float plateauHeight, AnimationCurve falloff) {
        TerrainData td = targetTerrain.terrainData;
        int w = td.heightmapResolution;
        int h = td.heightmapResolution;

        // convert world center to heightmap indices
        float ux = worldCenter.x / td.size.x;
        float uy = worldCenter.y / td.size.z;
        int cx = Mathf.RoundToInt(Mathf.Clamp01(ux) * (w - 1));
        int cy = Mathf.RoundToInt(Mathf.Clamp01(uy) * (h - 1));

        // determine affected radius in samples
        float samplePerUnitX = (w - 1) / td.size.x;
        float samplePerUnitZ = (h - 1) / td.size.z;
        int rX = Mathf.CeilToInt(radius * samplePerUnitX);
        int rY = Mathf.CeilToInt(radius * samplePerUnitZ);

        for (int y = cy - rY; y <= cy + rY; y++) {
            if (y < 0 || y >= h) continue;
            for (int x = cx - rX; x <= cx + rX; x++) {
                if (x < 0 || x >= w) continue;
                // compute distance in world units
                float wx = (float)x / (w - 1) * td.size.x;
                float wz = (float)y / (h - 1) * td.size.z;
                float dist = Vector2.Distance(new Vector2(wx, wz), worldCenter);
                if (dist > radius + 0.0001f) continue;
                float t = Mathf.Clamp01(dist / radius);
                float m = falloff.Evaluate(t); // 1 at center -> 0 at edge

                // plateauHeight is absolute world height; convert to normalized
                float normalizedPlateau = Mathf.Clamp01(plateauHeight / td.size.y);
                heights[x, y] = Mathf.Lerp(heights[x, y], normalizedPlateau, m);
            }
        }
    }

    void PostProcessSmoothing() {
        // Basic smoothing pass to reduce harsh transitions
        SmoothHeights(2);
    }

    void SmoothHeights(int iterations = 1) {
        TerrainData td = targetTerrain.terrainData;
        int w = td.heightmapResolution;
        int h = td.heightmapResolution;
        float[,] tmp = new float[w, h];
        Array.Copy(heights, tmp, heights.Length);

        for (int it = 0; it < iterations; it++) {
            for (int y = 1; y < h - 1; y++) {
                for (int x = 1; x < w - 1; x++) {
                    float sum = 0f; int cnt = 0;
                    for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++) { sum += tmp[x + ox, y + oy]; cnt++; }
                    heights[x, y] = sum / cnt;
                }
            }
            Array.Copy(heights, tmp, heights.Length);
        }
    }

    void ApplyHeightsAndLayersToTerrain() {
        
        var td = targetTerrain.terrainData;
        td.terrainLayers = null;
        if (td.terrainLayers == null || td.terrainLayers.Length == 0)
        {
            var layer = new TerrainLayer();
            layer.diffuseTexture = Texture2D.whiteTexture;
            layer.tileSize = new Vector2(30, 30);
            
            // Цвет можно задать через diffuseTexture или через Material
            // Если хотите просто цвет без текстуры:
            Texture2D colorTex = new Texture2D(1, 1);
            colorTex.SetPixel(0, 0, Color.darkOliveGreen);
            colorTex.Apply();
            layer.diffuseTexture = colorTex;
            
            td.terrainLayers = new TerrainLayer[] { layer };
            
            Debug.Log("TerrainWrapper: Created default TerrainLayer.");
        }

        td.SetHeights(0, 0, heights);
    }
}
