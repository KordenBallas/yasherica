using System;
using Combat.Battlefield;
using Combat.Config;

namespace Combat.Core
{
    /// <summary>
    /// Pure displacement geometry shared by execution and the outcome preview, so the ghost's
    /// predicted destination and the actual landing cell can never disagree.
    /// A push walks cell by cell and stops BEFORE the first invalid or occupied cell.
    /// </summary>
    public static class DisplacementResolver
    {
        public static HexCoordinates ResolveDestination(
            HexCoordinates from,
            HexDirection direction,
            int distance,
            HexDirectionConfig config,
            Func<HexCoordinates, bool> isCellValid,
            Func<HexCoordinates, bool> isCellOccupied)
        {
            var offset = FacingGeometry.OffsetFor(direction, config);
            var current = from;

            for (int step = 0; step < distance; step++)
            {
                var next = new HexCoordinates(current.Q + offset.Q, current.R + offset.R);

                if (!isCellValid(next) || isCellOccupied(next))
                    break;

                current = next;
            }

            return current;
        }
    }
}
