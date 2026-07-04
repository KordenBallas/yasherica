using System.Collections.Generic;
using LevelGeneration.Route;
using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Places the midground routing landmarks the route model calls for (the world objects a
    /// feature arc reads as routing around). The seam keeps <c>LevelGeneration.Route</c>
    /// UnityEngine-free: the generator hands over pure specs, the implementation owns the visuals.
    /// </summary>
    public interface IRouteLandmarkSpawner
    {
        /// <summary>Instantiates one world object per spec, parented under the area root.</summary>
        void Spawn(IReadOnlyList<LandmarkSpec> specs, Transform parent);
    }
}
