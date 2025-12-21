using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Registry for looking up platforms by position and managing platform-to-GameObject mapping.
    /// Used by character movement system to find current platform and navigate between platforms.
    /// </summary>
    public class PlatformRegistry : MonoBehaviour
    {
        private static PlatformRegistry instance;
        public static PlatformRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("PlatformRegistry");
                    instance = go.AddComponent<PlatformRegistry>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly Dictionary<int, IPlatform> platformsById = new();
        private readonly Dictionary<IPlatform, PlatformView> platformToView = new();
        private readonly Dictionary<GameObject, IPlatform> gameObjectToPlatform = new();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
            }
        }

        public void RegisterPlatform(IPlatform platform, PlatformView view)
        {
            if (platform == null || view == null) return;

            platformsById[platform.Id] = platform;
            platformToView[platform] = view;
            gameObjectToPlatform[view.gameObject] = platform;
        }

        public void UnregisterPlatform(IPlatform platform)
        {
            if (platform == null) return;

            platformsById.Remove(platform.Id);
            if (platformToView.TryGetValue(platform, out var view))
            {
                platformToView.Remove(platform);
                if (view != null && view.gameObject != null)
                {
                    gameObjectToPlatform.Remove(view.gameObject);
                }
            }
        }

        public IPlatform GetPlatformById(int id)
        {
            platformsById.TryGetValue(id, out var platform);
            return platform;
        }

        public IPlatform GetPlatformByGameObject(GameObject go)
        {
            // Check direct mapping
            if (gameObjectToPlatform.TryGetValue(go, out var platform))
            {
                return platform;
            }

            // Check parent hierarchy (platform view might be parent)
            Transform current = go.transform;
            while (current != null)
            {
                var view = current.GetComponent<PlatformView>();
                if (view != null && platformToView.ContainsValue(view))
                {
                    return platformToView.FirstOrDefault(kvp => kvp.Value == view).Key;
                }
                current = current.parent;
            }

            return null;
        }

        public IPlatform GetNearestPlatform(Vector3 worldPosition)
        {
            IPlatform best = null;
            float bestDist = float.MaxValue;

            foreach (var platform in platformsById.Values)
            {
                if (platform.Visual == null) continue;

                float dist = Vector3.Distance(worldPosition, platform.Visual.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = platform;
                }
            }

            return best;
        }

        public IPlatform GetPlatformAtPosition(Vector3 worldPosition, float maxDistance = 5f)
        {
            // Check if position is within any platform's boundary
            foreach (var platform in platformsById.Values)
            {
                if (platform.Visual == null || platform.Visual.TopBoundary == null) continue;

                // Check if point is inside platform boundary (XZ plane)
                if (IsPointInPolygonXZ(worldPosition, platform.Visual.TopBoundary))
                {
                    return platform;
                }
            }

            // Fallback to nearest platform within maxDistance
            var nearest = GetNearestPlatform(worldPosition);
            if (nearest != null)
            {
                float dist = Vector3.Distance(worldPosition, nearest.Visual.Position);
                if (dist <= maxDistance)
                {
                    return nearest;
                }
            }

            return null;
        }

        private bool IsPointInPolygonXZ(Vector3 point, IReadOnlyList<Vector3> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;

            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector3 pi = polygon[i];
                Vector3 pj = polygon[j];

                if (((pi.z > point.z) != (pj.z > point.z)) &&
                    (point.x < (pj.x - pi.x) * (point.z - pi.z) / (pj.z - pi.z) + pi.x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public void Clear()
        {
            platformsById.Clear();
            platformToView.Clear();
            gameObjectToPlatform.Clear();
        }
    }
}

