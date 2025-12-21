using System.Collections.Generic;
using UnityEngine;

namespace Battlefield
{
    public interface IHexGrid
    {
        HexOrientation Orientation { get; }
        float HexSize { get; }
        List<Vector3> Boundary { get; }
        
        void Initialize(List<Vector3> boundary, Vector3 center, float hexSize);
        List<HexCoordinates> GetCellsInBoundary();
        Vector3 HexToWorld(HexCoordinates hex);
        HexCoordinates WorldToHex(Vector3 world);
        bool IsCellInBoundary(HexCoordinates hex);
        void Clear();
    }
}

