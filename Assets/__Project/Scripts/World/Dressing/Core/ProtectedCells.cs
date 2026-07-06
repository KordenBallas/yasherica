using System.Collections.Generic;
using Combat.Battlefield;

namespace World.Dressing.Core
{
    /// <summary>
    /// The cells dressing must never block (biome-decoration brief FR8): the straight movement
    /// lane the hero hops platforms along (a band around local Z = 0 — platforms chain along +X
    /// and landings target the near-side cell), plus the center cell and its neighbors (the safe
    /// spawn/landing anchor <c>PlatformAnchor</c> relies on). Pure helper; no UnityEngine.
    /// </summary>
    public static class ProtectedCells
    {
        public static HashSet<HexCoordinates> Build(PlatformHexSurface surface, float laneHalfWidth)
        {
            var protectedSet = new HashSet<HexCoordinates> { surface.CenterCell };
            foreach (var offset in HexMetrics.NeighborOffsets)
            {
                protectedSet.Add(surface.CenterCell + offset);
            }

            foreach (var cell in surface.Cells)
            {
                var (_, z) = surface.GetCellCenterLocal(cell);
                if (z >= -laneHalfWidth && z <= laneHalfWidth)
                {
                    protectedSet.Add(cell);
                }
            }

            return protectedSet;
        }
    }
}
