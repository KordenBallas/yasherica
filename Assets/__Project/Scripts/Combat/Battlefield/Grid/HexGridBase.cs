using System.Collections.Generic;
using UnityEngine;

namespace Combat.Battlefield
{
    public abstract class HexGridBase : IHexGrid
    {
        public HexOrientation Orientation { get; protected set; }
        public float HexSize { get; protected set; }
        public List<Vector3> Boundary { get; protected set; } = new();
        
        protected Vector3 center;
        protected List<HexCoordinates> cellsInBoundary = new();
        protected Dictionary<HexCoordinates, Vector3> cellPositions = new(); // Store local positions (relative to center)
        
        public abstract void Initialize(List<Vector3> boundary, Vector3 center, float hexSize);
        public abstract List<HexCoordinates> GetCellsInBoundary();
        public abstract Vector3 HexToWorld(HexCoordinates hex);
        public abstract HexCoordinates WorldToHex(Vector3 world);
        public abstract bool IsCellInBoundary(HexCoordinates hex);
        
        protected abstract void CalculateCellsInBoundary();
        
        protected bool PointInPolygonXZ(Vector3 point, List<Vector3> poly)
        {
            if (poly == null || poly.Count < 3) return false;
            
            // Match legacy code exactly - ray casting algorithm (no division by zero check needed)
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vector3 pi = poly[i];
                Vector3 pj = poly[j];
                
                // Exact match to legacy code
                if (((pi.z > point.z) != (pj.z > point.z)) &&
                    (point.x < (pj.x - pi.x) * (point.z - pi.z) / (pj.z - pi.z) + pi.x))
                    inside = !inside;
            }
            return inside;
        }
        
        public virtual void Clear()
        {
            Boundary.Clear();
            cellsInBoundary.Clear();
            cellPositions.Clear();
        }
        
        public Vector3 GetCellPosition(HexCoordinates coords)
        {
            if (cellPositions.TryGetValue(coords, out var pos))
            {
                return pos;
            }
            // Fallback to HexToWorld if position not stored
            return HexToWorld(coords);
        }
    }
}

