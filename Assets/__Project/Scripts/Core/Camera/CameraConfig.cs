using UnityEngine;

namespace Core.Camera
{
    /// <summary>
    /// Configuration for combat camera parameters.
    /// ScriptableObject for easy tweaking without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Config/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Combat Camera Settings")]
        [Tooltip("Orthographic size for combat camera")]
        [SerializeField] private float _orthographicSize = 8f;
        
        [Tooltip("Damping on X axis for smoother camera following")]
        [SerializeField] private float _dampingX = 0.5f;
        
        [Tooltip("Damping on Y axis for smoother camera following")]
        [SerializeField] private float _dampingY = 0.5f;
        
        [Tooltip("Damping on Z axis for smoother camera following")]
        [SerializeField] private float _dampingZ = 0.5f;
        
        [Header("Transition Settings")]
        [Tooltip("Time in seconds for camera rotation transition")]
        [SerializeField] private float _transitionTime = 2f;
        
        [Header("Camera Rotations")]
        [Tooltip("Isometric camera rotation (default: X:30, Y:45, Z:0)")]
        [SerializeField] private Vector3 _isometricRotation = new Vector3(30f, 45f, 0f);
        
        [Tooltip("Combat camera rotation (default: X:45, Y:90, Z:0)")]
        [SerializeField] private Vector3 _combatRotation = new Vector3(45f, 90f, 0f);
        
        // Public getters
        public float OrthographicSize => _orthographicSize;
        public float DampingX => _dampingX;
        public float DampingY => _dampingY;
        public float DampingZ => _dampingZ;
        public float TransitionTime => _transitionTime;
        public Vector3 IsometricRotation => _isometricRotation;
        public Vector3 CombatRotation => _combatRotation;
    }
}

