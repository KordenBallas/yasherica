using System.Collections.Generic;
using Combat.Battlefield;
using UnityEngine;

namespace Platform
{
    public interface IPlatformVisual
    {
        Vector3 Position { get; set; }
        Vector2 Size { get; set; }

        /// <summary>
        /// The hex-cell composition of the top surface — the single source of truth the mesh, the
        /// colliders, and the combat grid all derive from.
        /// </summary>
        PlatformHexSurface Surface { get; set; }

        /// <summary>The walkable boundary polygon (local XZ) — the Surface outline as Vector3s.</summary>
        List<Vector3> TopBoundary { get; set; }
        // TODO: Revise exposure of GameObjects there. Needs refactoring.
        GameObject GameObject { get; set; }

        void UpdateVisual();
    }
}

