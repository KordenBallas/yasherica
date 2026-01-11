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
        [Tooltip("Offset vectors for 6 hex directions in axial coordinates")]
        public HexDirectionOffset[] directionOffsets = new HexDirectionOffset[6]
        {
            new HexDirectionOffset(HexDirection.E,  new Vector2Int(+1,  0)), // East
            new HexDirectionOffset(HexDirection.NE, new Vector2Int(+1, -1)), // Northeast
            new HexDirectionOffset(HexDirection.NW, new Vector2Int( 0, -1)), // Northwest
            new HexDirectionOffset(HexDirection.W,  new Vector2Int(-1,  0)), // West
            new HexDirectionOffset(HexDirection.SW, new Vector2Int(-1, +1)), // Southwest
            new HexDirectionOffset(HexDirection.SE, new Vector2Int( 0, +1)), // Southeast
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
