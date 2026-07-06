using System.Collections.Generic;
using Core.Logging;
using LevelGeneration;
using UnityEngine;
using World.Dressing.Core;
using World.Dressing.Data;

namespace World.Dressing.View
{
    /// <summary>
    /// Instantiates a platform's dressing plan: resolves each placement's (role, entry index)
    /// against the kit library, parents the instance under the platform (platform-local — dressing
    /// can never cross a gap), applies the bind-time tone treatment to every renderer, and strips
    /// colliders on decorative placements (blocking ones keep theirs so out-of-combat free-move
    /// respects the obstacle). Thin adapter: all placement decisions were made by the pure planner.
    /// </summary>
    public sealed class EnvironmentDressingSpawner : IEnvironmentDressingSpawner
    {
        private readonly DressingKitLibrary _library;
        private readonly ToneMaterialCache _toneCache;
        private readonly IGameLogger _logger;
        private readonly HashSet<string> _warnedKeys = new HashSet<string>();

        public EnvironmentDressingSpawner(
            DressingKitLibrary library, ToneMaterialCache toneCache, IGameLogger logger = null)
        {
            _library = library;
            _toneCache = toneCache;
            _logger = logger;
        }

        public Material ResolveGroundMaterial(PlatformDressingPlan plan)
        {
            if (plan == null || plan.Kind == DressingPlanKind.None)
            {
                return null;
            }

            Material ground = null;
            if (plan.Kind == DressingPlanKind.BiomeFeatures)
            {
                ground = _library?.GetBiomeKit(plan.KitKey)?.GroundMaterial;
            }
            else if (plan.Kind == DressingPlanKind.SiteDressing)
            {
                ground = _library?.GetSiteKit(plan.KitKey)?.GroundOverlayMaterial;
            }

            return ground != null ? _toneCache.GetToned(ground, plan.Theme) : null;
        }

        public void Spawn(PlatformDressingPlan plan, Transform platformTransform)
        {
            if (plan == null || plan.IsEmpty || platformTransform == null)
            {
                return;
            }

            var root = new GameObject("Dressing");
            root.transform.SetParent(platformTransform, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;

            foreach (var placement in plan.Placements)
            {
                if (!TryResolve(plan, placement, out GameObject prefab, out bool keepColliders))
                {
                    continue;
                }

                var instance = Object.Instantiate(prefab, root.transform);
                instance.transform.localPosition = new Vector3(placement.LocalX, 0f, placement.LocalZ);
                instance.transform.localRotation = Quaternion.Euler(0f, placement.YawDegrees, 0f);
                instance.transform.localScale = Vector3.one * placement.Scale;
                ApplyTone(instance, plan.Theme);
                if (!keepColliders)
                {
                    StripColliders(instance);
                }
            }
        }

        private bool TryResolve(
            PlatformDressingPlan plan,
            DressingPlacement placement,
            out GameObject prefab,
            out bool keepColliders)
        {
            prefab = null;
            keepColliders = false;

            if (plan.Kind == DressingPlanKind.BiomeFeatures)
            {
                var kit = _library?.GetBiomeKit(plan.KitKey);
                if (kit == null || placement.EntryIndex >= kit.Features.Count)
                {
                    WarnOnce($"biome-kit:{plan.KitKey}",
                        $"[EnvironmentDressingSpawner] Biome kit '{plan.KitKey}' unresolved — placements skipped.");
                    return false;
                }

                var entry = kit.Features[placement.EntryIndex];
                prefab = entry?.Prefab;
                keepColliders = entry != null && entry.Kind == FeatureKind.Blocking;
            }
            else if (plan.Kind == DressingPlanKind.SiteDressing)
            {
                var kit = _library?.GetSiteKit(plan.KitKey);
                if (kit == null)
                {
                    WarnOnce($"site-kit:{plan.KitKey}",
                        $"[EnvironmentDressingSpawner] Site kit '{plan.KitKey}' unresolved — placements skipped.");
                    return false;
                }

                var list = ListForRole(kit, placement.Role);
                if (list == null || list.Count == 0)
                {
                    return false;
                }

                prefab = list[placement.EntryIndex % list.Count];
                // Structures and the focal piece are the whole-cell blockers of the site planner.
                keepColliders = placement.Role == DressingRole.Structure
                    || placement.Role == DressingRole.Focal;
            }

            if (prefab == null)
            {
                WarnOnce($"prefab:{plan.KitKey}:{placement.Role}:{placement.EntryIndex}",
                    $"[EnvironmentDressingSpawner] Kit '{plan.KitKey}' {placement.Role} entry " +
                    $"{placement.EntryIndex} has no prefab — skipped.");
                return false;
            }

            return true;
        }

        private static IReadOnlyList<GameObject> ListForRole(SiteDressingKitDefinition kit, DressingRole role)
        {
            switch (role)
            {
                case DressingRole.Structure: return kit.Structures;
                case DressingRole.Prop: return kit.Props;
                case DressingRole.Focal: return kit.FocalProps;
                case DressingRole.Gate: return kit.GateProps;
                default: return null;
            }
        }

        private void ApplyTone(GameObject instance, LevelTheme theme)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        materials[i] = _toneCache.GetToned(materials[i], theme);
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
