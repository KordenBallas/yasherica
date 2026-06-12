using UnityEngine;

namespace Core.Camera
{
    /// <summary>
    /// Marker adapter placed on the hero's belly anchor transform so the camera
    /// framing can be resolved through IBellyAnchorProvider instead of a scene path.
    /// </summary>
    public class BellyAnchorMarker : MonoBehaviour, IBellyAnchorProvider
    {
        public Transform BellyAnchor => transform;
    }
}
