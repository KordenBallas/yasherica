using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Spawns the placeholder ability sweep (D3) across an ability's affected cells: one
    /// <see cref="AbilityCellFlash"/> per cell, its start delay staggered by distance from the caster
    /// for a Line (so the flashes read as a sweep outward along the line) and simultaneous for a Ring /
    /// area. The one authored motion, reused translucent by the ghost preview and opaque by the live
    /// playback (the caller passes the tint/alpha treatment). Placeholder — replaceable by real VFX.
    /// </summary>
    public static class AbilityAreaSweep
    {
        // For a line the last cell starts this fraction of the window after the first; a ring pops together.
        private const float LineStaggerFraction = 0.5f;

        /// <summary>
        /// Spawns the sweep and returns the created flash GameObjects (each self-destroys when its
        /// animation ends). Callers that replace their preview (the ghost) track and destroy them on
        /// stop; fire-and-forget callers (live) can ignore the list.
        /// </summary>
        public static List<GameObject> Play(
            Transform parent,
            IReadOnlyList<Vector3> cellPositions,
            Vector3 sweepOrigin,
            bool isLine,
            Color tint,
            float peakAlpha,
            float sweepSeconds,
            float cellSize)
        {
            var spawned = new List<GameObject>();
            if (parent == null || cellPositions == null || cellPositions.Count == 0)
                return spawned;

            // Order by distance from the caster so a line reads as a stable outward sweep.
            var ordered = cellPositions
                .OrderBy(pos => (pos - sweepOrigin).sqrMagnitude)
                .ToList();

            float maxStagger = isLine ? sweepSeconds * LineStaggerFraction : 0f;
            int count = ordered.Count;

            for (int i = 0; i < count; i++)
            {
                float delay = count <= 1 ? 0f : maxStagger * (i / (float)(count - 1));

                var flashGo = new GameObject("AbilityCellFlash");
                flashGo.transform.SetParent(parent, false);
                flashGo.AddComponent<AbilityCellFlash>()
                    .Initialize(ordered[i], cellSize, delay, tint, peakAlpha, sweepSeconds);
                spawned.Add(flashGo);
            }

            return spawned;
        }
    }
}
