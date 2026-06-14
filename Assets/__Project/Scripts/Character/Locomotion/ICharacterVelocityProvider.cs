using UnityEngine;

namespace Character.Locomotion
{
    /// <summary>
    /// Exposes the character's current planar (horizontal) velocity so the locomotion
    /// presenter can drive the run blend and facing without coupling to the concrete
    /// movement implementation.
    /// </summary>
    public interface ICharacterVelocityProvider
    {
        /// <summary>World-space horizontal velocity this frame (Y ignored); zero when idle.</summary>
        Vector3 PlanarVelocity { get; }

        /// <summary>Top planar speed, used to normalize <see cref="PlanarVelocity"/> for the blend.</summary>
        float MaxPlanarSpeed { get; }
    }
}
