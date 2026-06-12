using UnityEngine;

namespace Character
{
    /// <summary>
    /// Lets other subsystems turn the character toward a point of interest (e.g.
    /// the camera while the inventory is open) and later restore the original
    /// facing, without coupling to the movement implementation.
    /// </summary>
    public interface ICharacterFacing
    {
        /// <summary>Smoothly yaws the character toward the world point (Y is ignored).</summary>
        void FaceTowards(Vector3 worldPoint, float duration);

        /// <summary>Smoothly returns to the rotation stored by the first FaceTowards call.</summary>
        void RestoreFacing(float duration);
    }
}
