using Combat.Config;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Utility for converting world direction vectors to hex coordinates.
    /// Configuration-driven to support different hex orientations.
    /// </summary>
    public static class DirectionToHexConverter
    {
        /// <summary>
        /// Converts a world direction vector to the nearest hex neighbor.
        /// </summary>
        /// <param name="origin">The starting hex coordinate</param>
        /// <param name="worldDirection">Normalized direction in world space</param>
        /// <param name="config">Hex direction configuration</param>
        /// <returns>The hex coordinate of the neighbor in the specified direction</returns>
        public static HexCoordinates GetNeighborInDirection(
            HexCoordinates origin, 
            Vector3 worldDirection, 
            HexDirectionConfig config)
        {
            // Calculate angle of direction (in XZ plane)
            float angle = Mathf.Atan2(worldDirection.z, worldDirection.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            
            // Find closest hex direction based on angle
            HexDirection closestDir = FindClosestHexDirection(angle, config);
            
            // Get offset from config
            Vector2Int offset = GetOffsetForDirection(closestDir, config);
            
            return new HexCoordinates(origin.Q + offset.x, origin.R + offset.y);
        }
        
        /// <summary>
        /// Returns all 6 neighbor coordinates for a given origin.
        /// </summary>
        /// <param name="origin">The center hex coordinate</param>
        /// <param name="config">Hex direction configuration</param>
        /// <returns>Array of 6 neighbor hex coordinates</returns>
        public static HexCoordinates[] GetAllNeighbors(
            HexCoordinates origin, 
            HexDirectionConfig config)
        {
            var neighbors = new HexCoordinates[6];
            for (int i = 0; i < 6; i++)
            {
                var offset = config.directionOffsets[i].offset;
                neighbors[i] = new HexCoordinates(origin.Q + offset.x, origin.R + offset.y);
            }
            return neighbors;
        }
        
        private static HexDirection FindClosestHexDirection(float angle, HexDirectionConfig config)
        {
            // Divide 360° into 6 sectors (60° each for hex)
            // Adjust based on orientation
            float sectorSize = 60f;
            float startAngle = config.orientation == HexOrientation.Flat ? 0f : 30f;
            
            int sectorIndex = Mathf.RoundToInt((angle - startAngle) / sectorSize) % 6;
            if (sectorIndex < 0) sectorIndex += 6;
            
            return config.directionOffsets[sectorIndex].direction;
        }
        
        private static Vector2Int GetOffsetForDirection(HexDirection dir, HexDirectionConfig config)
        {
            foreach (var mapping in config.directionOffsets)
            {
                if (mapping.direction == dir)
                    return mapping.offset;
            }
            return Vector2Int.zero;
        }
    }
}
