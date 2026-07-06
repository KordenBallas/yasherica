using System.Collections.Generic;

namespace Hub.Core
{
    /// <summary>
    /// Pure proximity decision for the Hub's F-spots (O1 rework): of all spots whose interaction
    /// circle contains the player, pick the nearest — the one the prompt shows and F acts on.
    /// Planar (XZ) distances only, mirroring the NPC-interaction convention.
    /// </summary>
    public static class HubProximity
    {
        /// <summary>The index of the nearest in-radius spot, or -1 when none is in range.</summary>
        public static int FindNearest(float playerX, float playerZ, IReadOnlyList<HubInteractionSpot> spots)
        {
            int nearest = -1;
            float nearestSq = float.MaxValue;
            if (spots == null)
            {
                return nearest;
            }

            for (int i = 0; i < spots.Count; i++)
            {
                var spot = spots[i];
                if (spot == null)
                {
                    continue;
                }

                float dx = spot.X - playerX;
                float dz = spot.Z - playerZ;
                float distSq = dx * dx + dz * dz;
                if (distSq <= spot.Radius * spot.Radius && distSq < nearestSq)
                {
                    nearestSq = distSq;
                    nearest = i;
                }
            }

            return nearest;
        }
    }
}
