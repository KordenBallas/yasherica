using Combat.Battlefield;
using Combat.Config;

namespace Combat.Core
{
    /// <summary>
    /// Pure facing → hex-offset geometry over the authored HexDirectionConfig.
    /// The single place presentation and domain convert a HexDirection to axial space.
    /// </summary>
    public static class FacingGeometry
    {
        /// <summary>
        /// Axial (Q,R) offset for one step along the given facing.
        /// </summary>
        public static HexCoordinates OffsetFor(HexDirection direction, HexDirectionConfig config)
        {
            foreach (var mapping in config.directionOffsets)
            {
                if (mapping.direction == direction)
                    return new HexCoordinates(mapping.offset.x, mapping.offset.y);
            }
            return new HexCoordinates(0, 0);
        }

        /// <summary>
        /// The cell one step from <paramref name="from"/> along the given facing.
        /// </summary>
        public static HexCoordinates Neighbor(HexCoordinates from, HexDirection direction, HexDirectionConfig config)
        {
            var offset = OffsetFor(direction, config);
            return new HexCoordinates(from.Q + offset.Q, from.R + offset.R);
        }

        /// <summary>
        /// The facing that best points from <paramref name="from"/> toward <paramref name="to"/>:
        /// the direction whose one-step neighbor lands closest to the target (exact for adjacent
        /// cells, greedy-best for longer deltas). Null when the cells coincide. Ties break by
        /// authored config order, so the result is deterministic.
        /// </summary>
        public static HexDirection? DirectionFor(HexCoordinates from, HexCoordinates to, HexDirectionConfig config)
        {
            if (from.Q == to.Q && from.R == to.R)
                return null;

            HexDirection? best = null;
            int bestDistance = int.MaxValue;

            foreach (var mapping in config.directionOffsets)
            {
                var neighbor = new HexCoordinates(from.Q + mapping.offset.x, from.R + mapping.offset.y);
                int distance = AxialDistance(neighbor, to);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = mapping.direction;
                }
            }

            return best;
        }

        private static int AxialDistance(HexCoordinates a, HexCoordinates b)
        {
            int dq = System.Math.Abs(a.Q - b.Q);
            int dr = System.Math.Abs(a.R - b.R);
            int ds = System.Math.Abs((a.Q + a.R) - (b.Q + b.R));
            return (dq + dr + ds) / 2;
        }
    }
}
