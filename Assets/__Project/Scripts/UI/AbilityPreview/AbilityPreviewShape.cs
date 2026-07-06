using System.Collections.Generic;
using UnityEngine;

namespace UI.AbilityPreview
{
    /// <summary>
    /// Pure geometry for the preview stage's mock ground: local-space cell centres for a Line
    /// (straight ahead of the caster) or a Ring (the 6R-cell hex ring), on pointy-top axial
    /// hex metrics. No battlefield — the stage is illustrative, not the real board.
    /// </summary>
    public static class AbilityPreviewShape
    {
        private static readonly (int Q, int R)[] RingDirections =
        {
            (1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)
        };

        /// <summary>Cell centres for the previewed shape, relative to the caster at origin.</summary>
        public static List<Vector3> CellPositions(bool isLine, int lineLength, int ringRadius, float cellSize)
        {
            return isLine
                ? LinePositions(lineLength, cellSize)
                : RingPositions(ringRadius, cellSize);
        }

        private static List<Vector3> LinePositions(int length, float cellSize)
        {
            var positions = new List<Vector3>();
            for (int i = 1; i <= length; i++)
            {
                positions.Add(AxialToLocal(i, 0, cellSize));
            }

            return positions;
        }

        private static List<Vector3> RingPositions(int radius, float cellSize)
        {
            var positions = new List<Vector3>();
            if (radius <= 0)
            {
                return positions;
            }

            // Standard hex-ring walk: start radius cells out, then 6 sides of `radius` steps.
            int q = radius;
            int r = 0;
            for (int side = 0; side < RingDirections.Length; side++)
            {
                var direction = RingDirections[(side + 2) % RingDirections.Length];
                for (int step = 0; step < radius; step++)
                {
                    positions.Add(AxialToLocal(q, r, cellSize));
                    q += direction.Q;
                    r += direction.R;
                }
            }

            return positions;
        }

        private static Vector3 AxialToLocal(int q, int r, float cellSize)
        {
            const float Sqrt3 = 1.7320508f;
            float x = cellSize * Sqrt3 * (q + r * 0.5f);
            float z = cellSize * 1.5f * r;
            return new Vector3(x, 0f, z);
        }
    }
}
