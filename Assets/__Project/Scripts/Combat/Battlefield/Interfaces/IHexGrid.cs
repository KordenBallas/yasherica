using System.Collections.Generic;
using UnityEngine;

namespace Combat.Battlefield
{
    public interface IHexGrid
    {
        HexOrientation Orientation { get; }
        float HexSize { get; }

        /// <summary>The walkable outline polygon (local space), as carried by the surface.</summary>
        List<Vector3> Boundary { get; }

        /// <summary>Derives the grid from the platform's hex surface — the single source of truth.</summary>
        void Initialize(PlatformHexSurface surface, Vector3 center);
        List<HexCoordinates> GetCellsInBoundary();

        /// <summary>Local cell-center offset from the battlefield center.</summary>
        Vector3 GetCellPosition(HexCoordinates coords);
        Vector3 HexToWorld(HexCoordinates hex);
        HexCoordinates WorldToHex(Vector3 world);
        bool IsCellInBoundary(HexCoordinates hex);
        void Clear();
    }
}
