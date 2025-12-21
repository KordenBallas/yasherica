using UnityEngine;

namespace Battlefield
{
    public interface IHexCell
    {
        HexCoordinates Coordinates { get; }
        Vector3 WorldPosition { get; }
        Color Color { get; set; }
        bool IsActive { get; set; }
    }
}

