using Combat.Battlefield;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// World-space anchor points on a platform's walkable surface. Cell centers sit a full hex
    /// inradius inside the wall colliders, so landing or teleporting onto one can never strand a
    /// character outside the pen — unlike the raw centroid (<c>Visual.Position</c>), which on a
    /// concave union may fall outside every cell, or the mesh edge, which is the decorative rim
    /// beyond the walls.
    /// </summary>
    public static class PlatformAnchor
    {
        /// <summary>World position over the surface's center cell; the platform origin when no surface exists.</summary>
        public static Vector3 CenterCellWorld(PlatformHexSurface surface, Vector3 platformPosition)
        {
            if (surface == null)
            {
                return platformPosition;
            }

            var (x, z) = surface.GetCellCenterLocal(surface.CenterCell);
            return platformPosition + new Vector3(x, 0f, z);
        }

        /// <summary>
        /// World position over the walkable cell whose center is closest (in XZ) to
        /// <paramref name="worldPoint"/> — the near-side guaranteed-inside landing spot.
        /// </summary>
        public static Vector3 NearestCellWorld(PlatformHexSurface surface, Vector3 platformPosition, Vector3 worldPoint)
        {
            if (surface == null || surface.Cells.Count == 0)
            {
                return platformPosition;
            }

            float bestDistanceSq = float.MaxValue;
            float bestX = 0f;
            float bestZ = 0f;
            foreach (var cell in surface.Cells)
            {
                // A landing must never target a cell consumed by a dressing obstacle.
                if (surface.IsBlocked(cell))
                {
                    continue;
                }

                var (x, z) = surface.GetCellCenterLocal(cell);
                float dx = platformPosition.x + x - worldPoint.x;
                float dz = platformPosition.z + z - worldPoint.z;
                float distanceSq = dx * dx + dz * dz;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestX = x;
                    bestZ = z;
                }
            }

            return platformPosition + new Vector3(bestX, 0f, bestZ);
        }
    }
}
