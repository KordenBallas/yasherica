using System.Collections.Generic;
using LevelGeneration;
using LevelGeneration.Route;
using UnityEngine;
using World.Biomes.Data;

namespace World.Landscape
{
    /// <summary>
    /// Instantiates the midground routing landmarks the route model calls for: an authored kit
    /// prefab when the biome provides one, otherwise a procedural muted placeholder silhouette
    /// (peak for mountain/cave, dome for forest/desert) — the P5-4 asset pass swaps meshes in
    /// through the biome appearance asset with no code change. Landmarks are dressing only: no
    /// colliders, no shadows, parented under the area root so clearing the area clears them.
    /// </summary>
    public class RouteLandmarkSpawner : IRouteLandmarkSpawner
    {
        private static readonly Color DefaultLandmarkTint = new Color(0.47f, 0.52f, 0.47f);

        private readonly BiomeAppearanceDefinition _appearance;
        private readonly LevelTheme _theme;

        private Mesh _placeholderMesh;
        private Material _placeholderMaterial;

        public RouteLandmarkSpawner(BiomeAppearanceDefinition appearance, LevelTheme theme)
        {
            _appearance = appearance;
            _theme = theme;
        }

        public void Spawn(IReadOnlyList<LandmarkSpec> specs, Transform parent)
        {
            if (specs == null || parent == null)
            {
                return;
            }

            foreach (var spec in specs)
            {
                var position = new Vector3(spec.X, spec.Y, spec.Z);
                GameObject landmark = CreateLandmark(spec);
                landmark.transform.SetParent(parent, worldPositionStays: false);
                landmark.transform.position = position;
                landmark.transform.localScale = Vector3.one * spec.Scale;
            }
        }

        private GameObject CreateLandmark(LandmarkSpec spec)
        {
            var kit = _appearance != null ? _appearance.LandmarkKit : null;
            if (kit != null && kit.Count > 0)
            {
                var prefab = kit[spec.KitIndex % kit.Count];
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab);
                    instance.name = $"RouteLandmark_{spec.X:F0}";
                    return instance;
                }
            }

            var landmark = new GameObject($"RouteLandmark_{spec.X:F0}");
            var filter = landmark.AddComponent<MeshFilter>();
            var renderer = landmark.AddComponent<MeshRenderer>();
            filter.sharedMesh = GetPlaceholderMesh();
            renderer.sharedMaterial = GetPlaceholderMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return landmark;
        }

        private Mesh GetPlaceholderMesh()
        {
            if (_placeholderMesh == null)
            {
                Color tint = _appearance != null ? _appearance.LandmarkTint : DefaultLandmarkTint;
                bool pointed = _theme == LevelTheme.Mountain || _theme == LevelTheme.Cave;
                _placeholderMesh = pointed
                    ? PlaceholderSilhouetteMeshBuilder.BuildPeak(tint)
                    : PlaceholderSilhouetteMeshBuilder.BuildDome(tint);
            }

            return _placeholderMesh;
        }

        private Material GetPlaceholderMaterial()
        {
            if (_placeholderMaterial == null)
            {
                // Unlit + vertex-color driven; the muted tint is baked into the mesh.
                _placeholderMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            return _placeholderMaterial;
        }
    }
}
