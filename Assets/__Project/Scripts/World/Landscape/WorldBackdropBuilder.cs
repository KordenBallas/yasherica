using LevelGeneration.Route;
using UnityEngine;
using World.Biomes.Data;

namespace World.Landscape
{
    /// <summary>
    /// Assembles the distant world backdrop rig (world-backdrop-and-elevation brief, FR C): two
    /// hazed ridge strips from <see cref="BackdropSilhouetteModel"/> plus a sky gradient quad,
    /// rotated to face the fixed isometric camera. Authored silhouette meshes from the biome
    /// appearance asset replace the procedural strips when present (the P5-4 seam). The rig is
    /// muted and unlit — lowest in the focus hierarchy; a per-Site backdrop is a nearer layer by
    /// construction (finite world position vs. this screen-fixed horizon).
    /// </summary>
    public class WorldBackdropBuilder
    {
        // Rig layout in local space: +Z is "away from the camera" after the yaw rotation. Each part
        // is LOWERED by distance·tan(pitch): under the tilted orthographic camera a distant object
        // at ground height projects 0.5·distance ABOVE the visible window (no perspective
        // convergence), so the horizon must descend with its distance — after the drop, a part's
        // on-screen height depends only on its base offset, never on how far away it sits.
        private const float FarRidgeDistance = 260f;
        private const float NearRidgeDistance = 180f;
        private const float SkyDistance = 340f;
        private const float FarRidgeWidth = 900f;
        private const float NearRidgeWidth = 700f;
        private const float SkyWidth = 1200f;
        private const float SkyHeight = 220f;
        private const float SkyBaseOffset = -60f;
        private const float FarRidgeBaseOffset = -2f;
        private const float NearRidgeBaseOffset = -4f;
        private const float RidgeBaseDrop = 30f;
        private const int RidgeSampleCount = 96;
        private const float FarRidgeHeightScale = 6f;
        private const float NearRidgeHeightScale = 3.6f;
        private const float AuthoredMeshSpacing = 60f;

        // Fallbacks mirroring the BiomeAppearanceDefinition inspector defaults, for unauthored biomes.
        private static readonly Color DefaultSkyTopTint = new Color(0.62f, 0.71f, 0.76f);
        private static readonly Color DefaultSkyHorizonTint = new Color(0.82f, 0.85f, 0.84f);
        private static readonly Color DefaultHazeTint = new Color(0.75f, 0.79f, 0.79f);

        public GameObject Build(
            BiomeAppearanceDefinition appearance,
            BiomeLandscapeSettings settings,
            int seed,
            float cameraYawDegrees,
            float cameraPitchDegrees)
        {
            Color skyTop = appearance != null ? appearance.SkyTopTint : DefaultSkyTopTint;
            Color skyHorizon = appearance != null ? appearance.SkyHorizonTint : DefaultSkyHorizonTint;
            Color haze = appearance != null ? appearance.HazeTint : DefaultHazeTint;

            var root = new GameObject("WorldBackdrop");
            root.transform.rotation = Quaternion.Euler(0f, cameraYawDegrees, 0f);

            var material = new Material(Shader.Find("Sprites/Default"));
            var model = new BackdropSilhouetteModel(settings, seed);
            float pitchDrop = Mathf.Tan(cameraPitchDegrees * Mathf.Deg2Rad);

            // Far layer sits deepest in the haze and taller, so its peaks show above the nearer,
            // slightly darker layer — two separating silhouettes that never compete with gameplay.
            AddRidgeLayer(root, appearance, model, material,
                layerIndex: 0, FarRidgeDistance, FarRidgeWidth,
                -FarRidgeDistance * pitchDrop + FarRidgeBaseOffset, FarRidgeHeightScale,
                Color.Lerp(haze, Color.black, 0.07f), haze);
            AddRidgeLayer(root, appearance, model, material,
                layerIndex: 1, NearRidgeDistance, NearRidgeWidth,
                -NearRidgeDistance * pitchDrop + NearRidgeBaseOffset, NearRidgeHeightScale,
                Color.Lerp(haze, Color.black, 0.18f), haze);

            var sky = CreatePart("SkyBand", root.transform, material,
                PlaceholderSilhouetteMeshBuilder.BuildSkyQuad(SkyWidth, SkyHeight, skyTop, skyHorizon));
            sky.transform.localPosition = new Vector3(
                0f, -SkyDistance * pitchDrop + SkyBaseOffset, SkyDistance);

            return root;
        }

        private static void AddRidgeLayer(
            GameObject root,
            BiomeAppearanceDefinition appearance,
            BackdropSilhouetteModel model,
            Material material,
            int layerIndex,
            float distance,
            float width,
            float baseHeight,
            float heightScale,
            Color ridgeTint,
            Color hazeTint)
        {
            var kit = appearance != null ? appearance.SilhouetteKit : null;
            if (kit != null && kit.Count > 0)
            {
                // Authored silhouettes: tile the kit along the layer. Heights/spacing stay simple;
                // the real composition pass belongs to the decoration pipeline (P5-4).
                int count = Mathf.Max(1, Mathf.FloorToInt(width / AuthoredMeshSpacing));
                for (int i = 0; i < count; i++)
                {
                    var mesh = kit[i % kit.Count];
                    if (mesh == null)
                    {
                        continue;
                    }

                    var part = CreatePart($"RidgeMesh_{layerIndex}_{i}", root.transform, material, mesh);
                    float x = (i + 0.5f) / count * width - width * 0.5f;
                    part.transform.localPosition = new Vector3(x, baseHeight, distance);
                }

                return;
            }

            float[] heights = model.GetRidgeHeights(layerIndex, RidgeSampleCount, width);
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] *= heightScale;
            }

            var strip = CreatePart($"RidgeStrip_{layerIndex}", root.transform, material,
                PlaceholderSilhouetteMeshBuilder.BuildRidgeStrip(
                    heights, width, RidgeBaseDrop, ridgeTint, hazeTint));
            strip.transform.localPosition = new Vector3(0f, baseHeight, distance);
        }

        private static GameObject CreatePart(string name, Transform parent, Material material, Mesh mesh)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, worldPositionStays: false);
            var filter = part.AddComponent<MeshFilter>();
            var renderer = part.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }
    }
}
