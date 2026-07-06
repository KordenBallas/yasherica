using UnityEngine;

namespace UI.AbilityPreview
{
    /// <summary>
    /// All tunables of the shared ability-preview stage (camera framing, light, mock ground,
    /// sweep treatment, loop pacing). Configuration only — the rig reads it, never writes.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityPreviewConfig", menuName = "Yasherica/UI/Ability Preview Config")]
    public class AbilityPreviewConfig : ScriptableObject
    {
        [Header("Stage placement (far from every gameplay camera)")]
        [SerializeField] private Vector3 _rigWorldOffset = new Vector3(4000f, 0f, -4000f);

        [Header("Render texture")]
        [SerializeField] private int _textureSize = 384;

        [Header("Camera framing")]
        [SerializeField] private float _cameraHeight = 1.6f;
        [SerializeField] private float _cameraDistance = 3.4f;
        [SerializeField] private float _lookAtHeight = 0.9f;
        [SerializeField] private float _fieldOfView = 40f;
        [SerializeField] private float _farClipPlane = 50f;
        [SerializeField] private Color _backgroundColor = new Color(0.09f, 0.08f, 0.10f, 1f);
        [Tooltip("Yaw of the hero model so the cast reads three-quarter, not dead-on.")]
        [SerializeField] private float _modelYawDegrees = 200f;

        [Header("Light")]
        [SerializeField] private Vector3 _lightLocalPosition = new Vector3(1.5f, 2.5f, -1.5f);
        [SerializeField] private float _lightRange = 12f;
        [SerializeField] private float _lightIntensity = 1.4f;

        [Header("Mock ground")]
        [SerializeField] private float _groundSize = 8f;
        [SerializeField] private Color _groundColor = new Color(0.16f, 0.15f, 0.17f, 1f);

        [Header("Cell sweep (reuses the D3 placeholder treatment)")]
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField] private Color _sweepTint = new Color(1f, 0.55f, 0.2f, 1f);
        [Range(0f, 1f)]
        [SerializeField] private float _sweepPeakAlpha = 0.85f;
        [SerializeField] private float _sweepSeconds = 0.6f;
        [Tooltip("How often the cast demonstration repeats while the popover is open.")]
        [SerializeField] private float _loopIntervalSeconds = 1.4f;

        public Vector3 RigWorldOffset => _rigWorldOffset;
        public int TextureSize => _textureSize;
        public float CameraHeight => _cameraHeight;
        public float CameraDistance => _cameraDistance;
        public float LookAtHeight => _lookAtHeight;
        public float FieldOfView => _fieldOfView;
        public float FarClipPlane => _farClipPlane;
        public Color BackgroundColor => _backgroundColor;
        public float ModelYawDegrees => _modelYawDegrees;
        public Vector3 LightLocalPosition => _lightLocalPosition;
        public float LightRange => _lightRange;
        public float LightIntensity => _lightIntensity;
        public float GroundSize => _groundSize;
        public Color GroundColor => _groundColor;
        public float CellSize => _cellSize;
        public Color SweepTint => _sweepTint;
        public float SweepPeakAlpha => _sweepPeakAlpha;
        public float SweepSeconds => _sweepSeconds;
        public float LoopIntervalSeconds => _loopIntervalSeconds;
    }
}
