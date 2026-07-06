using System.Collections.Generic;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// The combat grid derived 1:1 from a platform's <see cref="PlatformHexSurface"/> — the cells ARE
    /// the ground tiles the platform was built from, so nothing is re-fitted or re-snapped at combat
    /// time (brief §1). Replaces the legacy boundary-scan grids (<c>FlatHexGrid</c>/<c>PointyHexGrid</c>).
    /// </summary>
    public class SurfaceHexGrid : IHexGrid
    {
        private PlatformHexSurface _surface;
        private Vector3 _center;
        private readonly List<HexCoordinates> _cells = new();
        private readonly Dictionary<HexCoordinates, Vector3> _cellPositions = new(); // local offsets

        public HexOrientation Orientation { get; private set; }
        public float HexSize { get; private set; }
        public List<Vector3> Boundary { get; private set; } = new();

        public void Initialize(PlatformHexSurface surface, Vector3 center)
        {
            _surface = surface;
            _center = center;
            Orientation = surface.Orientation;
            HexSize = surface.HexSize;

            _cells.Clear();
            _cellPositions.Clear();
            foreach (var cell in surface.Cells)
            {
                // Blocked cells (dressing obstacles) are ground/mesh but never combat cells: every
                // consumer (movement, targeting, spawn placement) routes through this grid.
                if (surface.IsBlocked(cell))
                {
                    continue;
                }

                var (x, z) = surface.GetCellCenterLocal(cell);
                _cells.Add(cell);
                _cellPositions[cell] = new Vector3(x, 0f, z);
            }

            Boundary = new List<Vector3>(surface.Outline.Count);
            foreach (var (x, z) in surface.Outline)
            {
                Boundary.Add(new Vector3(x, 0f, z));
            }
        }

        public List<HexCoordinates> GetCellsInBoundary()
        {
            return _cells;
        }

        /// <summary>Local cell-center offset from the battlefield center.</summary>
        public Vector3 GetCellPosition(HexCoordinates coords)
        {
            if (_cellPositions.TryGetValue(coords, out var localPos))
            {
                return localPos;
            }

            return HexToWorld(coords) - _center;
        }

        public Vector3 HexToWorld(HexCoordinates hex)
        {
            if (_cellPositions.TryGetValue(hex, out var localPos))
            {
                return new Vector3(localPos.x, 0f, localPos.z) + _center;
            }

            // Off-surface coordinates still map consistently through the shared metrics.
            var (x, z) = _surface.GetCellCenterLocal(hex);
            return new Vector3(x, 0f, z) + _center;
        }

        public HexCoordinates WorldToHex(Vector3 world)
        {
            // Exact inverse of cell placement: undo the center + centroid recentering, then round.
            Vector3 local = world - _center;
            var (q, r) = HexMetrics.LocalToFractional(
                local.x + _surface.CenterOffset.X,
                local.z + _surface.CenterOffset.Z,
                Orientation, HexSize);
            return HexMetrics.Round(q, r);
        }

        public bool IsCellInBoundary(HexCoordinates hex)
        {
            return _surface != null && _surface.Contains(hex) && !_surface.IsBlocked(hex);
        }

        public void Clear()
        {
            _surface = null;
            _cells.Clear();
            _cellPositions.Clear();
            Boundary.Clear();
        }
    }
}
