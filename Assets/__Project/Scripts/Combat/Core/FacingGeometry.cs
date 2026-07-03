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
    }
}
