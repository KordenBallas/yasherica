using System.Collections.Generic;
using UnityEngine;

namespace Platform
{
    public static class PlatformColliderBuilder
    {
        public static void BuildPlatformColliders(
            GameObject platformObj,
            List<Vector3> outlinePoints, // XZ points describing platform perimeter
            float platformThickness,
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
            floorCol.center = new Vector3(center.x, -platformThickness - floorThickness * 0.5f, center.z);

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
                int wallLayer = LayerMask.NameToLayer("WallLayer");
                if (wallLayer == -1) wallLayer = LayerMask.NameToLayer("Default");
                if (wallLayer == -1) wallLayer = 0;
                wall.layer = wallLayer;
                wall.transform.SetParent(root.transform, false);

                // Move to middle of the segment
                wall.transform.localPosition = new Vector3(mid.x, wallHeight * 0.5f, mid.z);

                // Rotate the wall to align with the edge
                Vector3 dir = (b - a).normalized;
                if (dir.sqrMagnitude > 0.001f)
                {
                    wall.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }

                var wallCol = wall.AddComponent<BoxCollider>();
                wallCol.size = new Vector3(wallThickness, wallHeight, segmentLength);
            }
        }
    }
}

