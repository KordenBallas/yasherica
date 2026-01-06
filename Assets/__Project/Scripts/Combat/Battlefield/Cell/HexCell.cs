using UnityEngine;

namespace Combat.Battlefield
{
    public class HexCell : IHexCell
    {
        public HexCoordinates Coordinates { get; }
        public Vector3 WorldPosition { get; }
        public Color Color { get; set; }
        public bool IsActive { get; set; }
        
        public HexCell(HexCoordinates coordinates, Vector3 worldPosition)
        {
            Coordinates = coordinates;
            WorldPosition = worldPosition;
            Color = Color.white;
            IsActive = true;
        }
    }
}

