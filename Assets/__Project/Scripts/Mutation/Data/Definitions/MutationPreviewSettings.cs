using System;
using UnityEngine;

namespace Mutation.Data.Definitions
{
    /// <summary>
    /// Tunables for the mutation card's mini-model preview rig (the hover popover showing
    /// the hero wearing the offered part). Configuration only - the rig reads these; no
    /// logic lives here. Field initializers double as the defaults for existing
    /// MutationConfig assets that predate the block.
    /// </summary>
    [Serializable]
    public class MutationPreviewSettings
    {
        [Tooltip("Square render-texture resolution of the preview")]
        [Min(32)]
        [SerializeField] private int _textureSize = 256;

        [Tooltip("Where the preview rig lives in world space - far away so no gameplay camera ever sees it")]
        [SerializeField] private Vector3 _rigWorldOffset = new Vector3(0f, -500f, 0f);

        [Tooltip("Camera offset from the model anchor: forward distance")]
        [Min(0.1f)]
        [SerializeField] private float _cameraDistance = 2.5f;

        [Tooltip("Camera offset from the model anchor: height")]
        [SerializeField] private float _cameraHeight = 1.4f;

        [Tooltip("Height on the model the camera looks at (roughly the chest)")]
        [SerializeField] private float _lookAtHeight = 0.9f;

        [Tooltip("Preview camera vertical field of view")]
        [Range(1f, 120f)]
        [SerializeField] private float _fieldOfView = 30f;

        [Tooltip("Preview camera far clip plane - keep small so the rig sees only the model")]
        [Min(1f)]
        [SerializeField] private float _farClipPlane = 25f;

        [Tooltip("Solid background colour behind the model")]
        [SerializeField] private Color _backgroundColor = new Color(0.09f, 0.09f, 0.11f, 1f);

        [Tooltip("Yaw applied to the preview model so it faces the camera (placeholder art faces -Z)")]
        [SerializeField] private float _modelYawDegrees = 180f;

        [Tooltip("Point light position relative to the model anchor")]
        [SerializeField] private Vector3 _lightLocalPosition = new Vector3(1.5f, 2f, 1.5f);

        [Tooltip("Preview point light range")]
        [Min(0f)]
        [SerializeField] private float _lightRange = 8f;

        [Tooltip("Preview point light intensity")]
        [Min(0f)]
        [SerializeField] private float _lightIntensity = 1.2f;

        public int TextureSize => _textureSize;
        public Vector3 RigWorldOffset => _rigWorldOffset;
        public float CameraDistance => _cameraDistance;
        public float CameraHeight => _cameraHeight;
        public float LookAtHeight => _lookAtHeight;
        public float FieldOfView => _fieldOfView;
        public float FarClipPlane => _farClipPlane;
        public Color BackgroundColor => _backgroundColor;
        public float ModelYawDegrees => _modelYawDegrees;
        public Vector3 LightLocalPosition => _lightLocalPosition;
        public float LightRange => _lightRange;
        public float LightIntensity => _lightIntensity;
    }
}
