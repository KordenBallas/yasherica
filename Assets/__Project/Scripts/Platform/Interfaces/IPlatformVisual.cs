using System.Collections.Generic;
using UnityEngine;

namespace Platform
{
    public interface IPlatformVisual
    {
        Vector3 Position { get; set; }
        Vector2 Size { get; set; }
        List<Vector3> TopBoundary { get; set; }
        // TODO: Revise exposure of GameObjects there. Needs refactoring.
        GameObject GameObject { get; set; }
        
        void UpdateVisual();
    }
}

