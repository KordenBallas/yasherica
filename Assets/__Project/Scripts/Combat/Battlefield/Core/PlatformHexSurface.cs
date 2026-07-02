using System;
using System.Collections.Generic;

namespace Combat.Battlefield
{
    /// <summary>
    /// The single source of truth for a platform's walkable ground: the set of whole hex cells the
    /// top surface is composed of, plus the derived walkable outline and the decorative rim ring.
    /// Built once at generation time; the mesh, the colliders, and the combat grid all consume this
    /// same object, which is what makes "the combat grid is literally the ground" true by
    /// construction (platform-hex-surface-and-shape brief §1). Immutable after construction; pure C#.
    /// </summary>
    public sealed class PlatformHexSurface
    {
        private readonly HashSet<HexCoordinates> _cellSet;

        /// <param name="cells">The whole cells of the top surface; stored sorted by (Q, R).</param>
        /// <param name="orientation">Hex orientation shared with the combat grid.</param>
        /// <param name="hexSize">Hex size shared with the combat grid.</param>
        /// <param name="centerOffset">Subtracted from raw axial centers so the cell-union centroid
        /// sits at the platform's local origin (the platform Position stays the visual center).</param>
        /// <param name="outline">The hex-union boundary polygon (local XZ) — the walkable edge.</param>
        /// <param name="subdividedOutline">The outline with edge midpoints inserted; index-aligned
        /// with <paramref name="rimRing"/> (the decorative rim strip runs between the two).</param>
        /// <param name="rimRing">The organic decorative outer ring (local XZ) — dressing only,
        /// never walkable, never a combat cell.</param>
        public PlatformHexSurface(
            IReadOnlyList<HexCoordinates> cells,
            HexOrientation orientation,
            float hexSize,
            (float X, float Z) centerOffset,
            IReadOnlyList<(float X, float Z)> outline,
            IReadOnlyList<(float X, float Z)> subdividedOutline,
            IReadOnlyList<(float X, float Z)> rimRing)
        {
            if (cells == null || cells.Count == 0)
            {
                throw new ArgumentException("A platform surface needs at least one cell.", nameof(cells));
            }

            if (subdividedOutline == null || rimRing == null || subdividedOutline.Count != rimRing.Count)
            {
                throw new ArgumentException("Rim ring must be index-aligned with the subdivided outline.");
            }

            var sorted = new List<HexCoordinates>(cells);
            sorted.Sort(CompareCells);
            Cells = sorted;
            _cellSet = new HashSet<HexCoordinates>(sorted);
            Orientation = orientation;
            HexSize = hexSize;
            CenterOffset = centerOffset;
            Outline = outline ?? throw new ArgumentNullException(nameof(outline));
            SubdividedOutline = subdividedOutline;
            RimRing = rimRing;
            CenterCell = FindCenterCell();
        }

        /// <summary>The whole cells of the top surface, sorted by (Q, R) for determinism.</summary>
        public IReadOnlyList<HexCoordinates> Cells { get; }

        public HexOrientation Orientation { get; }

        public float HexSize { get; }

        /// <summary>Centroid offset applied to every cell center so the union is centered at origin.</summary>
        public (float X, float Z) CenterOffset { get; }

        /// <summary>The cell whose center is nearest the local origin — a safe spawn/center point
        /// even when the union is concave and the raw centroid falls outside every cell.</summary>
        public HexCoordinates CenterCell { get; }

        /// <summary>The hex-union boundary polygon (local XZ, counter-clockwise) — the walkable edge
        /// the colliders and the platform-detection polygon test use.</summary>
        public IReadOnlyList<(float X, float Z)> Outline { get; }

        /// <summary>The outline with edge midpoints inserted; pairs 1:1 with <see cref="RimRing"/>.</summary>
        public IReadOnlyList<(float X, float Z)> SubdividedOutline { get; }

        /// <summary>The decorative organic rim ring — dressing only, outside the walkable edge.</summary>
        public IReadOnlyList<(float X, float Z)> RimRing { get; }

        /// <summary>
        /// Cells unavailable to movement/combat. Always empty today — the documented seam for the
        /// biome-features brief (features occupy whole cells; brief §9).
        /// </summary>
        public IReadOnlyCollection<HexCoordinates> BlockedCells { get; } = Array.Empty<HexCoordinates>();

        public bool Contains(HexCoordinates hex) => _cellSet.Contains(hex);

        /// <summary>Local XZ center of a cell, recentered by <see cref="CenterOffset"/>.</summary>
        public (float X, float Z) GetCellCenterLocal(HexCoordinates hex)
        {
            var (x, z) = HexMetrics.CellCenter(hex, Orientation, HexSize);
            return (x - CenterOffset.X, z - CenterOffset.Z);
        }

        private HexCoordinates FindCenterCell()
        {
            HexCoordinates best = Cells[0];
            float bestSq = float.MaxValue;
            foreach (var cell in Cells)
            {
                var (x, z) = GetCellCenterLocal(cell);
                float sq = x * x + z * z;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = cell;
                }
            }

            return best;
        }

        private static int CompareCells(HexCoordinates a, HexCoordinates b)
        {
            int byQ = a.Q.CompareTo(b.Q);
            return byQ != 0 ? byQ : a.R.CompareTo(b.R);
        }
    }
}
