using UnityEngine;
using World.Dressing.Core;

namespace LevelGeneration
{
    /// <summary>
    /// Instantiates a platform's dressing plan (environment-dressing). The seam keeps the area
    /// generator decoupled from the dressing visuals, mirroring <see cref="IRouteLandmarkSpawner"/>:
    /// the generator hands over the pure plan, the implementation owns prefabs, materials, and the
    /// bind-time tone treatment.
    /// </summary>
    public interface IEnvironmentDressingSpawner
    {
        /// <summary>
        /// The platform's toned ground material per the plan's kit (site overlay or biome ground);
        /// null = keep the default platform material (base layer).
        /// </summary>
        Material ResolveGroundMaterial(PlatformDressingPlan plan);

        /// <summary>Instantiates every placement of the plan under the platform transform.</summary>
        void Spawn(PlatformDressingPlan plan, Transform platformTransform);
    }
}
