using System.Collections.Generic;
using Core.Camera;
using Core.Logging;
using LevelGeneration;
using UnityEngine;
using World.Biomes;
using World.Dressing.Core;
using World.Dressing.Data;

namespace World.Dressing.View
{
    /// <summary>
    /// Instantiates the distant-scatter horizon (Track E4): resolves the active biome's backdrop
    /// kit, plans the span through the pure <see cref="BackdropScatterPlanner"/>, and places the
    /// silhouettes world-fixed under the area root — far behind the field (real parallax as the
    /// hero moves, unlike the hero-anchored ridge strips), lowered by distance·tan(pitch) so the
    /// tilted orthographic camera reads them as a horizon, washed toward the biome haze tint
    /// (the quietest layer), colliders stripped (never walkable, gaps stay clean hops).
    /// Thin adapter: all placement decisions come from the pure planner.
    /// </summary>
    public sealed class BackdropScatterSpawner : IBackdropScatterSpawner
    {
        /// <summary>Base vertical offset after the ortho drop (cf. the ridge strips' −2/−4).</summary>
        private const float BaseOffset = -2f;

        private readonly IBiomeAppearanceCatalog _appearanceCatalog;
        private readonly Loot.Core.ICurrentThemeProvider _themeProvider;
        private readonly Loot.Core.IRunSeedProvider _seedProvider;
        private readonly ToneMaterialCache _toneCache;
        private readonly IGameLogger _logger;
        private readonly BackdropScatterPlanner _planner = new BackdropScatterPlanner();
        private readonly float _pitchDrop;
        private readonly float _yawSin;
        private readonly float _yawCos;
        private readonly Dictionary<BackdropKitDefinition, IReadOnlyList<BackdropEntryData>> _pools =
            new Dictionary<BackdropKitDefinition, IReadOnlyList<BackdropEntryData>>();
        private readonly HashSet<string> _warnedKeys = new HashSet<string>();

        public BackdropScatterSpawner(
            IBiomeAppearanceCatalog appearanceCatalog,
            Loot.Core.ICurrentThemeProvider themeProvider,
            Loot.Core.IRunSeedProvider seedProvider,
            ToneMaterialCache toneCache,
            CameraConfig cameraConfig,
            IGameLogger logger = null)
        {
            _appearanceCatalog = appearanceCatalog;
            _themeProvider = themeProvider;
            _seedProvider = seedProvider;
            _toneCache = toneCache;
            _logger = logger;
            // The same ortho compensation the backdrop rig applies: a distant object must descend
            // by distance·tan(pitch) or the tilted ortho camera lifts it out of frame forever —
            // and "distant" means along the CAMERA's yawed depth axis, not raw world +Z (the
            // isometric yaw is 45°; a pure-Z offset would land both below and beside the frame).
            float pitchDegrees = cameraConfig != null ? cameraConfig.IsometricRotation.x : 0f;
            float yawDegrees = cameraConfig != null ? cameraConfig.IsometricRotation.y : 0f;
            _pitchDrop = Mathf.Tan(pitchDegrees * Mathf.Deg2Rad);
            _yawSin = Mathf.Sin(yawDegrees * Mathf.Deg2Rad);
            _yawCos = Mathf.Cos(yawDegrees * Mathf.Deg2Rad);
        }

        public void Fill(float fromX, float toXExclusive, Transform parent)
        {
            if (parent == null || toXExclusive <= fromX)
            {
                return;
            }

            var theme = _themeProvider.CurrentTheme;
            var appearance = _appearanceCatalog?.Get(theme);
            var kit = appearance != null ? appearance.BackdropKit : null;
            if (kit == null)
            {
                // No backdrop kit bound = the ridge strips stay the only horizon (fail-safe).
                return;
            }

            if (!_pools.TryGetValue(kit, out var entries))
            {
                entries = DressingKitMapper.ToBackdropEntries(kit, _logger);
                _pools[kit] = entries;
            }

            var placements = _planner.PlanSpan(
                fromX, toXExclusive, entries, appearance.BackdropScatterPer100Units,
                _seedProvider?.RunSeed ?? 0);
            if (placements.Count == 0)
            {
                return;
            }

            var root = new GameObject($"BackdropScatter_{Mathf.FloorToInt(fromX)}");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;

            foreach (var placement in placements)
            {
                var prefab = kit.Entries[placement.EntryIndex % kit.Entries.Count]?.Prefab;
                if (prefab == null)
                {
                    WarnOnce($"prefab:{kit.KitId}:{placement.EntryIndex}",
                        $"[BackdropScatterSpawner] Backdrop kit '{kit.KitId}' entry " +
                        $"{placement.EntryIndex} has no prefab — skipped.");
                    continue;
                }

                var instance = Object.Instantiate(prefab, root.transform);
                // World position = run anchor (X) + the planner's depth pushed along the camera's
                // yawed axis, dropped by depth·tan(pitch). On screen this cancels to: the item
                // reads at run-position X, its pivot just under the hero's ground line, its
                // silhouette rising above — between the platforms and the ridge strips.
                instance.transform.localPosition = new Vector3(
                    placement.X + placement.Z * _yawSin,
                    -placement.Z * _pitchDrop + BaseOffset,
                    placement.Z * _yawCos);
                instance.transform.localRotation = Quaternion.Euler(0f, placement.YawDegrees, 0f);
                instance.transform.localScale = Vector3.one * placement.Scale;
                ApplyHaze(instance, theme);
                StripColliders(instance);
            }
        }

        private void ApplyHaze(GameObject instance, LevelTheme theme)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        materials[i] = _toneCache.GetHazed(materials[i], theme);
                    }
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void StripColliders(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.Destroy(collider);
            }
        }

        private void WarnOnce(string key, string message)
        {
            if (_warnedKeys.Add(key))
            {
                _logger?.Warning(LogCategory.LevelGeneration, message);
            }
        }
    }
}
