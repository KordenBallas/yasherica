using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Combat.Battlefield
{
    public class Battlefield : IBattlefield
    {
        private IHexGrid grid;
        private bool isActive;
        private float hexSize;
        private Vector3 center;
        
        public bool IsActive => isActive;
        public float HexSize => hexSize;
        public Vector3 Center => center;
        public IHexGrid Grid => grid;
        
        public void Initialize(List<Vector3> boundary, Vector3 center, float hexSize, HexOrientation orientation)
        {
            this.hexSize = hexSize;
            this.center = center;
            
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
    }
}

