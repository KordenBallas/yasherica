using Combat.Battlefield;
using UnityEngine;

namespace Combat.Config
{
    /// <summary>
    /// Configuration for hex grid direction mappings and orientation.
    /// </summary>
    [CreateAssetMenu(fileName = "HexDirectionConfig", menuName = "Combat/Hex Direction Config")]
    public class HexDirectionConfig : ScriptableObject
    {
        [Header("Orientation")]
        [Tooltip("Hex grid orientation (flat-top or pointy-top)")]
        public HexOrientation orientation = HexOrientation.Flat;
        
        [Header("Direction Mappings (Flat-Top Hex)")]
        [Tooltip("Offset vectors for 6 hex directions in axial coordinates, ordered by sector index")]
        public HexDirectionOffset[] directionOffsets = new HexDirectionOffset[6]
        {
            // Array index maps directly to sector index from DirectionToHexConverter
            // Order empirically determined based on actual world coordinate behavior
            new HexDirectionOffset(HexDirection.NW, new Vector2Int( 0, -1)), // Index 0 - Northwest (sector 0)
            new HexDirectionOffset(HexDirection.NE, new Vector2Int(+1, -1)), // Index 1 - Northeast (sector 1)
            new HexDirectionOffset(HexDirection.E,  new Vector2Int(+1,  0)), // Index 2 - East (sector 2)
            new HexDirectionOffset(HexDirection.SE, new Vector2Int( 0, +1)), // Index 3 - Southeast (sector 3)
            new HexDirectionOffset(HexDirection.SW, new Vector2Int(-1, +1)), // Index 4 - Southwest (sector 4)
            new HexDirectionOffset(HexDirection.W,  new Vector2Int(-1,  0)), // Index 5 - West (sector 5)
        };
        
        [Header("World Direction Thresholds")]
        [Tooltip("Angle range in degrees for each hex direction")]
        public float directionAngleThreshold = 30f;
    }
    
    [System.Serializable]
    public class HexDirectionOffset
    {
        public HexDirection direction;
        public Vector2Int offset; // Q, R in axial coordinates
        
        public HexDirectionOffset(HexDirection dir, Vector2Int off)
        {
            direction = dir;
            offset = off;
        }
    }
    
    public enum HexDirection
    {
        E,  // East
        NE, // Northeast
        NW, // Northwest
        W,  // West
        SW, // Southwest
        SE  // Southeast
    }
}
