using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Computes affected hex cells for Line and Ring ability shapes.
    /// Single source of truth used by both the highlight preview and the executor.
    /// </summary>
    public class AbilityShapeCalculator : IAbilityShapeCalculator
    {
        private readonly HexDirectionConfig _config;

        public AbilityShapeCalculator(HexDirectionConfig config)
        {
            _config = config;
        }

        public IReadOnlyList<HexCoordinates> GetAffectedCells(
            AbilityShapeData shape,
            HexCoordinates casterPosition,
            HexDirection? direction,
            Func<HexCoordinates, bool> isCellInBoundary)
        {
            return shape.Type switch
            {
                AbilityShapeType.Line => GetLineCells(shape, casterPosition, direction, isCellInBoundary),
                AbilityShapeType.Ring => GetRingCells(shape, casterPosition, isCellInBoundary),
                _ => new List<HexCoordinates>()
            };
        }

        private IReadOnlyList<HexCoordinates> GetLineCells(
            AbilityShapeData shape,
            HexCoordinates casterPosition,
            HexDirection? direction,
            Func<HexCoordinates, bool> isCellInBoundary)
        {
            if (!direction.HasValue)
                return new List<HexCoordinates>();

            var result = new List<HexCoordinates>(shape.LineLength);
            var offset = GetOffsetForDirection(direction.Value);
            var current = casterPosition;

            for (int i = 0; i < shape.LineLength; i++)
            {
                current = new HexCoordinates(current.Q + offset.x, current.R + offset.y);

                if (!isCellInBoundary(current))
                    break;

                result.Add(current);
            }

            return result;
        }

        private IReadOnlyList<HexCoordinates> GetRingCells(
            AbilityShapeData shape,
            HexCoordinates casterPosition,
            Func<HexCoordinates, bool> isCellInBoundary)
        {
            int radius = shape.RingRadius;
            var result = new List<HexCoordinates>();

            for (int q = -radius; q <= radius; q++)
            {
                int rMin = System.Math.Max(-radius, -q - radius);
                int rMax = System.Math.Min(radius, -q + radius);

                for (int r = rMin; r <= rMax; r++)
                {
                    var coords = new HexCoordinates(casterPosition.Q + q, casterPosition.R + r);

                    if (CalculateDistance(casterPosition, coords) != radius)
                        continue;

                    if (!isCellInBoundary(coords))
                        continue;

                    result.Add(coords);
                }
            }

            return result;
        }

        private Vector2Int GetOffsetForDirection(HexDirection direction)
        {
            foreach (var mapping in _config.directionOffsets)
            {
                if (mapping.direction == direction)
                    return mapping.offset;
            }
            return Vector2Int.zero;
        }

        private static int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            int dq = System.Math.Abs(from.Q - to.Q);
            int dr = System.Math.Abs(from.R - to.R);
            int ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));
            return (dq + dr + ds) / 2;
        }
    }
}
