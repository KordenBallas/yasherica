using System.Collections.Generic;
using UnityEngine;

namespace Platform
{
    public class PlatformVisual : IPlatformVisual
    {
        public Vector3 Position { get; set; }
        public Vector2 Size { get; set; }
        public List<Vector3> TopBoundary { get; set; } = new();
        public GameObject GameObject { get; set; } 
        
        public void UpdateVisual()
        {
            // Update visual representation if needed
            // This can be called when visual properties change
        }
    }
}

