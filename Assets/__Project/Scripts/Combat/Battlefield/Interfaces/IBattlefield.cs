using System.Collections.Generic;
using UnityEngine;

namespace Combat.Battlefield
{
    public interface IBattlefield
    {
        bool IsActive { get; }
        float HexSize { get; }
        Vector3 Center { get; }
        IHexGrid Grid { get; } // Expose grid for accessing GetCellPosition
        
        void Initialize(List<Vector3> boundary, Vector3 center, float hexSize, HexOrientation orientation);
        void Activate();
        void Deactivate();
        void Clear();
        
        // Grid access through Battlefield interface (encapsulated)
        IHexCell GetCellAt(HexCoordinates coordinates);
        IReadOnlyList<IHexCell> GetCellsInRange(HexCoordinates center, int range);
        IReadOnlyList<HexCoordinates> GetCellsInBoundary();
        Vector3 HexToWorld(HexCoordinates hex);
        HexCoordinates WorldToHex(Vector3 world);
        bool IsCellInBoundary(HexCoordinates hex);
    }
}

