using System;

namespace Combat.Battlefield
{
    /// <summary>
    /// Pure hex-grid math shared by the platform surface generation and the combat grid — the single
    /// place the axial-coordinate ↔ local-position conversion lives, so the ground and the battlefield
    /// can never disagree. The flat/pointy center formulas are exact ports of the legacy
    /// <c>FlatHexGrid.HexToWorld</c> / <c>PointyHexGrid.HexToWorld</c> fallbacks (the compatibility
    /// contract for anything authored against the old grids). No UnityEngine.
    /// </summary>
    public static class HexMetrics
    {
        private const float Sqrt3 = 1.7320508f;

        /// <summary>
        /// The six axial neighbor directions, in the edge order the outline extractor relies on:
        /// edge <c>i</c> of a cell (between <see cref="Corner"/> <c>i</c> and <c>i+1</c>) borders the
        /// neighbor at <c>NeighborOffsets[i]</c>, for both orientations.
        /// </summary>
        public static readonly HexCoordinates[] NeighborOffsets =
        {
            new HexCoordinates(1, 0),
            new HexCoordinates(0, 1),
            new HexCoordinates(-1, 1),
            new HexCoordinates(-1, 0),
            new HexCoordinates(0, -1),
            new HexCoordinates(1, -1)
        };

        public const int CornerCount = 6;

        /// <summary>Local XZ center of a cell (no recentering applied).</summary>
        public static (float X, float Z) CellCenter(HexCoordinates hex, HexOrientation orientation, float size)
        {
            if (orientation == HexOrientation.Flat)
            {
                return (size * 1.5f * hex.Q, size * Sqrt3 * (hex.R + 0.5f * hex.Q));
            }

            return (size * Sqrt3 * (hex.Q + 0.5f * hex.R), size * 1.5f * hex.R);
        }

        /// <summary>
        /// Corner <c>i</c> offset from the cell center, counter-clockwise (x-right / z-up sense).
        /// Flat-top corners sit at 60°·i; pointy-top at 60°·i − 30°, which keeps
        /// <see cref="NeighborOffsets"/> edge-aligned for both orientations.
        /// </summary>
        public static (float X, float Z) Corner(int index, HexOrientation orientation, float size)
        {
            float degrees = orientation == HexOrientation.Flat ? 60f * index : 60f * index - 30f;
            double radians = degrees * Math.PI / 180.0;
            return (size * (float)Math.Cos(radians), size * (float)Math.Sin(radians));
        }

        /// <summary>Inverse of <see cref="CellCenter"/> into fractional axial coordinates.</summary>
        public static (float Q, float R) LocalToFractional(float x, float z, HexOrientation orientation, float size)
        {
            if (orientation == HexOrientation.Flat)
            {
                float q = (2f / 3f * x) / size;
                float r = (-1f / 3f * x + Sqrt3 / 3f * z) / size;
                return (q, r);
            }

            float pq = (Sqrt3 / 3f * x - 1f / 3f * z) / size;
            float pr = (2f / 3f * z) / size;
            return (pq, pr);
        }

        /// <summary>
        /// Cube-rounds fractional axial coordinates to the containing cell (port of the legacy
        /// <c>HexCoordinatesFromFractional</c>).
        /// </summary>
        public static HexCoordinates Round(float q, float r)
        {
            float s = -q - r;
            // MidpointRounding.ToEven matches UnityEngine.Mathf.RoundToInt (round half to even),
            // keeping this an exact port of the legacy grid rounding.
            int qInt = (int)Math.Round(q, MidpointRounding.ToEven);
            int rInt = (int)Math.Round(r, MidpointRounding.ToEven);
            int sInt = (int)Math.Round(s, MidpointRounding.ToEven);

            float qDiff = Math.Abs(qInt - q);
            float rDiff = Math.Abs(rInt - r);
            float sDiff = Math.Abs(sInt - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                qInt = -rInt - sInt;
            }
            else if (rDiff > sDiff)
            {
                rInt = -qInt - sInt;
            }

            return new HexCoordinates(qInt, rInt);
        }
    }
}
