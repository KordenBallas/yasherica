using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Battlefield
{
    public class FlatHexGrid : HexGridBase
    {
        public FlatHexGrid()
        {
            Orientation = HexOrientation.Flat;
        }
        
        public class Factory : PlaceholderFactory<FlatHexGrid>
        {
        }
        
        public override void Initialize(List<Vector3> boundary, Vector3 center, float hexSize)
        {
            Boundary = boundary;
            this.center = center;
            HexSize = hexSize;
            CalculateCellsInBoundary();
        }
        
        public override List<HexCoordinates> GetCellsInBoundary()
        {
            return cellsInBoundary;
        }
        
        protected override void CalculateCellsInBoundary()
        {
            cellsInBoundary.Clear();
            
            if (Boundary == null || Boundary.Count < 3)
            {
                return;
            }
            
            // Boundary is in local space (relative to center at origin)
            // Calculate bounding box in local space (matching legacy code approach)
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in Boundary)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }
            
            // Flat-top hex dimensions (matching legacy code)
            float hexWidth = HexSize * 2f;
            float hexHeight = Mathf.Sqrt(3f) * HexSize;
            
            // Use HashSet for efficient duplicate checking
            HashSet<HexCoordinates> seen = new HashSet<HexCoordinates>();
            
            // Iterate through hex grid covering bounding rectangle (matching legacy code exactly)
            // Note: We iterate in local space, but the algorithm is the same
            for (float x = minX - hexWidth; x <= maxX + hexWidth; x += hexWidth * 0.75f)
            {
                for (float z = minZ - hexHeight; z <= maxZ + hexHeight; z += hexHeight)
                {
                    // Start with base position (matching legacy: Vector3 cell = new Vector3(x, platformCenter.y, z))
                    Vector3 cellLocalPos = new Vector3(x, 0f, z);
                    
                    // Offset each second column for hex staggering (matching legacy calculation exactly)
                    int colIndex = (int)((x - minX) / (hexWidth * 0.75f));
                    if (colIndex % 2 == 1)
                    {
                        cellLocalPos.z += hexHeight * 0.5f;
                    }
                    
                    // Check if cell center is inside polygon (both in local space, matching legacy logic)
                    // Legacy: PointInPolygonXZ(cell, platformBoundary) where both are in world space
                    // Our: PointInPolygonXZ(cellLocalPos, Boundary) where both are in local space
                    if (!PointInPolygonXZ(cellLocalPos, Boundary))
                        continue;
                    
                    // Convert local position to world for hex coordinate calculation
                    Vector3 cellWorldPos = cellLocalPos + center;
                    HexCoordinates coords = WorldToHex(cellWorldPos);
                    
                    // Avoid duplicates and store local position (offset from center)
                    if (seen.Add(coords))
                    {
                        cellsInBoundary.Add(coords);
                        cellPositions[coords] = cellLocalPos; // Store local offset
                    }
                }
            }
        }
        
        public override Vector3 HexToWorld(HexCoordinates hex)
        {
            // Flat-top hex to world conversion
            float x = HexSize * (1.5f * hex.Q);
            float z = HexSize * (Mathf.Sqrt(3f) * (hex.R + 0.5f * hex.Q));
            return new Vector3(x, center.y, z) + center;
        }
        
        public override HexCoordinates WorldToHex(Vector3 world)
        {
            // World to flat-top hex conversion
            Vector3 localPos = world - center;
            float q = (2f / 3f * localPos.x) / HexSize;
            float r = (-1f / 3f * localPos.x + Mathf.Sqrt(3f) / 3f * localPos.z) / HexSize;
            return HexCoordinatesFromFractional(q, r);
        }
        
        public override bool IsCellInBoundary(HexCoordinates hex)
        {
            // Convert to local space for polygon check (boundary is in local space)
            Vector3 worldPos = HexToWorld(hex);
            Vector3 localPos = worldPos - center;
            return PointInPolygonXZ(localPos, Boundary);
        }
        
        private HexCoordinates HexCoordinatesFromFractional(float q, float r)
        {
            float s = -q - r;
            int qInt = Mathf.RoundToInt(q);
            int rInt = Mathf.RoundToInt(r);
            int sInt = Mathf.RoundToInt(s);
            
            float qDiff = Mathf.Abs(qInt - q);
            float rDiff = Mathf.Abs(rInt - r);
            float sDiff = Mathf.Abs(sInt - s);
            
            if (qDiff > rDiff && qDiff > sDiff)
            {
                qInt = -rInt - sInt;
            }
            else if (rDiff > sDiff)
            {
                rInt = -qInt - sInt;
            }
            
            return new HexCoordinates(qInt, rInt);
        }
    }
}

