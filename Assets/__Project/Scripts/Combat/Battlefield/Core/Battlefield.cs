using System.Collections.Generic;
using System.Linq;
using Combat.Config;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Defines the type of cell selection pattern.
    /// </summary>
    public enum CellSelectionType
    {
        Single,      // Single target cell
        Sector,      // Cone-shaped area
        Line,        // Linear pattern
        Area,        // Circular area
        Adjacent     // Immediate neighbors (6 cells)
    }

    /// <summary>
    /// Parameters for cell selection operations.
    /// Different selection types require different parameters.
    /// </summary>
    public struct CellSelectionParams
    {
        public HexCoordinates Origin;           // Character position
        public Vector3? Direction;              // Normalized direction (required for Sector/Line)
        public int Range;                       // Max distance for Sector
        public float ConeAngle;                 // Degrees for Sector (e.g., 60, 90, 120)
        public int LineLength;                  // Number of cells for Line
        public int LineWidth;                   // Cells perpendicular to line (1 = single)
        public int Radius;                      // For Area selection
        public bool StopOnObstacle;             // Stop at occupied cells (for Line/Sector)
    }

    public class Battlefield : IBattlefield
    {
        private IHexGrid grid;
        private bool isActive;
        private float hexSize;
        private Vector3 center;
        private HexDirectionConfig hexConfig;

        public bool IsActive => isActive;
        public float HexSize => hexSize;
        public Vector3 Center => center;
        public IHexGrid Grid => grid;

        public void Initialize(List<Vector3> boundary, Vector3 center, float hexSize, HexOrientation orientation, HexDirectionConfig hexConfig)
        {
            this.hexSize = hexSize;
            this.center = center;
            this.hexConfig = hexConfig;

            // Create grid based on orientation
            if (orientation == HexOrientation.Flat)
            {
                grid = new FlatHexGrid();
            }
            else
            {
                grid = new PointyHexGrid();
            }
            
            grid.Initialize(boundary, center, hexSize);
            
            // Pre-create all cells in boundary
            PreCreateCellsInBoundary();
        }
        
        private void PreCreateCellsInBoundary()
        {
            cells.Clear();

            var cellsInBoundary = grid.GetCellsInBoundary();
            foreach (var coords in cellsInBoundary)
            {
                // Get local position (offset from center) and convert to world for storage
                Vector3 localPos = grid is HexGridBase gridBase
                    ? gridBase.GetCellPosition(coords)
                    : (grid.HexToWorld(coords) - center);
                Vector3 worldPos = localPos + center;
                var cell = new HexCell(coords, worldPos);

                // Initialize state machine with idle state
                cell.InitializeStateMachine(new HexCellIdleState());

                cells[coords] = cell;
            }
        }
        
        public void Activate()
        {
            isActive = true;
        }
        
        public void Deactivate()
        {
            isActive = false;
        }
        
        public void Clear()
        {
            grid?.Clear();
            cells.Clear();
            isActive = false;
        }
        
        private Dictionary<HexCoordinates, IHexCell> cells = new();
        
        public IHexCell GetCellAt(HexCoordinates coordinates)
        {
            if (cells.TryGetValue(coordinates, out var cell))
            {
                return cell;
            }

            // Create new cell if it's in boundary
            if (grid != null && grid.IsCellInBoundary(coordinates))
            {
                Vector3 worldPos = grid.HexToWorld(coordinates);
                var newCell = new HexCell(coordinates, worldPos);

                // Initialize state machine with idle state
                newCell.InitializeStateMachine(new HexCellIdleState());

                cells[coordinates] = newCell;
                return newCell;
            }

            return null;
        }
        
        public IReadOnlyList<IHexCell> GetCellsInRange(HexCoordinates center, int range)
        {
            var result = new List<IHexCell>();
            
            for (int q = -range; q <= range; q++)
            {
                int r1 = Mathf.Max(-range, -q - range);
                int r2 = Mathf.Min(range, -q + range);
                for (int r = r1; r <= r2; r++)
                {
                    var coords = new HexCoordinates(center.Q + q, center.R + r);
                    var cell = GetCellAt(coords);
                    if (cell != null)
                    {
                        result.Add(cell);
                    }
                }
            }
            
            return result.AsReadOnly();
        }
        
        public IReadOnlyList<HexCoordinates> GetCellsInBoundary()
        {
            if (grid == null) return new List<HexCoordinates>().AsReadOnly();
            return grid.GetCellsInBoundary().AsReadOnly();
        }
        
        public Vector3 HexToWorld(HexCoordinates hex)
        {
            if (grid == null) return Vector3.zero;
            return grid.HexToWorld(hex);
        }
        
        public HexCoordinates WorldToHex(Vector3 world)
        {
            if (grid == null) return new HexCoordinates(0, 0);
            return grid.WorldToHex(world);
        }
        
        public bool IsCellInBoundary(HexCoordinates hex)
        {
            if (grid == null) return false;
            return grid.IsCellInBoundary(hex);
        }

        /// <summary>
        /// Gets cells based on selection type and parameters.
        /// </summary>
        public IReadOnlyList<IHexCell> GetCellsBySelection(CellSelectionType selectionType, CellSelectionParams parameters)
        {
            return selectionType switch
            {
                CellSelectionType.Single => GetSingleCell(parameters),
                CellSelectionType.Sector => GetCellsInSector(parameters),
                CellSelectionType.Line => GetCellsInLine(parameters),
                CellSelectionType.Area => GetCellsInArea(parameters),
                CellSelectionType.Adjacent => GetAdjacentCells(parameters),
                _ => new List<IHexCell>().AsReadOnly()
            };
        }

        private IReadOnlyList<IHexCell> GetSingleCell(CellSelectionParams parameters)
        {
            var cell = GetCellAt(parameters.Origin);
            return cell != null
                ? new List<IHexCell> { cell }.AsReadOnly()
                : new List<IHexCell>().AsReadOnly();
        }

        private IReadOnlyList<IHexCell> GetAdjacentCells(CellSelectionParams parameters)
        {
            // Get all cells within range 1, but exclude origin
            var cellsInRange = GetCellsInRange(parameters.Origin, 1);
            var result = new List<IHexCell>();

            foreach (var cell in cellsInRange)
            {
                if (!cell.Coordinates.Equals(parameters.Origin))
                {
                    result.Add(cell);
                }
            }

            return result.AsReadOnly();
        }

        private IReadOnlyList<IHexCell> GetCellsInArea(CellSelectionParams parameters)
        {
            // Reuse existing GetCellsInRange method
            return GetCellsInRange(parameters.Origin, parameters.Radius);
        }

        private IReadOnlyList<IHexCell> GetCellsInSector(CellSelectionParams parameters)
        {
            if (!parameters.Direction.HasValue || hexConfig == null)
                return new List<IHexCell>().AsReadOnly();

            var result = new List<IHexCell>();
            Vector3 direction = parameters.Direction.Value;

            // Get all cells within range as candidates
            var candidates = GetCellsInRange(parameters.Origin, parameters.Range);

            Vector3 originWorld = HexToWorld(parameters.Origin);

            foreach (var cell in candidates)
            {
                // Skip origin cell
                if (cell.Coordinates.Equals(parameters.Origin))
                    continue;

                // Calculate vector from origin to cell
                Vector3 cellWorld = cell.WorldPosition;
                Vector3 toCell = (cellWorld - originWorld);

                // Project onto XZ plane and normalize
                Vector3 directionXZ = new Vector3(direction.x, 0, direction.z).normalized;
                Vector3 toCellXZ = new Vector3(toCell.x, 0, toCell.z).normalized;

                // Calculate angle between direction and toCell vectors
                float angle = Vector3.Angle(directionXZ, toCellXZ);

                // Check if within cone angle
                if (angle <= parameters.ConeAngle / 2f)
                {
                    result.Add(cell);

                    // If stop on obstacle, check if this cell is occupied
                    if (parameters.StopOnObstacle &&
                        cell.StateMachine.IsInState(HexCellStateType.Occupied))
                    {
                        break;
                    }
                }
            }

            return result.AsReadOnly();
        }

        private IReadOnlyList<IHexCell> GetCellsInLine(CellSelectionParams parameters)
        {
            if (!parameters.Direction.HasValue || hexConfig == null)
                return new List<IHexCell>().AsReadOnly();

            var result = new List<IHexCell>();
            Vector3 direction = parameters.Direction.Value;
            HexCoordinates current = parameters.Origin;

            for (int i = 0; i < parameters.LineLength; i++)
            {
                // Get next cell in direction
                current = DirectionToHexConverter.GetNeighborInDirection(current, direction, hexConfig);

                var cell = GetCellAt(current);
                if (cell == null || !IsCellInBoundary(current))
                    break;

                result.Add(cell);

                // Handle line width > 1 (perpendicular cells)
                if (parameters.LineWidth > 1)
                {
                    var perpCells = GetPerpendicularCells(current, direction, parameters.LineWidth);
                    result.AddRange(perpCells);
                }

                // Stop on obstacle
                if (parameters.StopOnObstacle &&
                    cell.StateMachine.IsInState(HexCellStateType.Occupied))
                {
                    break;
                }
            }

            return result.AsReadOnly();
        }

        private IEnumerable<IHexCell> GetPerpendicularCells(HexCoordinates center, Vector3 direction, int width)
        {
            if (hexConfig == null) yield break;

            // Calculate perpendicular direction (rotate 90 degrees on XZ plane)
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x).normalized;

            for (int i = 1; i <= width / 2; i++)
            {
                // Positive perpendicular direction
                var coords1 = DirectionToHexConverter.GetNeighborInDirection(center, perpendicular, hexConfig);
                var cell1 = GetCellAt(coords1);
                if (cell1 != null && IsCellInBoundary(coords1))
                    yield return cell1;

                // Negative perpendicular direction
                var coords2 = DirectionToHexConverter.GetNeighborInDirection(center, -perpendicular, hexConfig);
                var cell2 = GetCellAt(coords2);
                if (cell2 != null && IsCellInBoundary(coords2))
                    yield return cell2;
            }
        }
    }
}

