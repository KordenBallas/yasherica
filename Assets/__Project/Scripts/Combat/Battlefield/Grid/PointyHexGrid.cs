using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Combat.Battlefield
{
    public class PointyHexGrid : HexGridBase
    {
        public PointyHexGrid()
        {
            Orientation = HexOrientation.Pointy;
        }
        
        public class Factory : PlaceholderFactory<PointyHexGrid>
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
            
            // Boundary is already in local space (relative to center)
            // Calculate bounding box in local space
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in Boundary)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }
            
            // Pointy-top hex dimensions
            float hexWidth = Mathf.Sqrt(3f) * HexSize;
            float hexHeight = HexSize * 2f;
            
            // Use HashSet for efficient duplicate checking
            HashSet<HexCoordinates> seen = new HashSet<HexCoordinates>();
            
            // Iterate through hex grid covering bounding rectangle (in local space)
            for (float x = minX - hexWidth; x <= maxX + hexWidth; x += hexWidth)
            {
                for (float z = minZ - hexHeight; z <= maxZ + hexHeight; z += hexHeight * 0.75f)
                {
                    // Offset each second row for hex staggering (pointy-top)
                    int rowIndex = (int)((z - (minZ - hexHeight)) / (hexHeight * 0.75f));
                    float xOffset = (rowIndex % 2 == 1) ? hexWidth * 0.5f : 0f;
                    
                    Vector3 cellLocalPos = new Vector3(x + xOffset, 0f, z);
                    
                    // Check if cell center is inside polygon (boundary is in local space)
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
            // Use stored position if available (ensures character is at actual cell center)
            if (cellPositions.TryGetValue(hex, out var localPos))
            {
                return new Vector3(localPos.x, center.y, localPos.z) + center;
            }

            // Fallback: Pointy-top hex to world conversion
            float x = HexSize * (Mathf.Sqrt(3f) * hex.Q + Mathf.Sqrt(3f) / 2f * hex.R);
            float z = HexSize * (1.5f * hex.R);
            return new Vector3(x, center.y, z) + center;
        }
        
        public override HexCoordinates WorldToHex(Vector3 world)
        {
            // World to pointy-top hex conversion
            Vector3 localPos = world - center;
            float q = (Mathf.Sqrt(3f) / 3f * localPos.x - 1f / 3f * localPos.z) / HexSize;
            float r = (2f / 3f * localPos.z) / HexSize;
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

