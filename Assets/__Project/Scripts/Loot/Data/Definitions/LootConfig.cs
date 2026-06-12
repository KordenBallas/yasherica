using UnityEngine;

namespace Loot.Data.Definitions
{
    /// <summary>
    /// Global loot tunables: pickup animation timings, world artifact visuals,
    /// spawn placement and roll modifiers. Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "LootConfig", menuName = "Loot/Loot Config")]
    public class LootConfig : ScriptableObject
    {
        [Header("Pickup Animation")]
        [Tooltip("Total pickup animation duration in seconds")]
        [SerializeField] private float _pickupDuration = 0.5f;

        [Tooltip("Peak scale multiplier of the grow-then-shrink animation")]
        [SerializeField] private float _pickupGrowScale = 1.3f;

        [Tooltip("Fraction of the duration spent growing before shrinking to zero")]
        [Range(0.05f, 0.95f)]
        [SerializeField] private float _pickupPeakFraction = 0.4f;

        [Tooltip("Fade the artifact out while it shrinks")]
        [SerializeField] private bool _pickupFadeEnabled = true;

        [Tooltip("Move the artifact towards the player during pickup")]
        [SerializeField] private bool _pickupMoveToPlayer = true;

        [Header("World Artifact Visuals")]
        [Tooltip("Number of rear sprite layers behind the front icon (pseudo-3D depth)")]
        [SerializeField] private int _depthLayerCount = 4;

        [Tooltip("Local distance between consecutive depth layers")]
        [SerializeField] private float _depthLayerSpacing = 0.015f;

        [Tooltip("How much the rearmost layer is darkened (0 = no darkening)")]
        [Range(0f, 1f)]
        [SerializeField] private float _depthLayerDarkening = 0.45f;

        [Tooltip("Diameter of the blob shadow relative to the icon size")]
        [SerializeField] private float _shadowScale = 0.8f;

        [Range(0f, 1f)]
        [SerializeField] private float _shadowAlpha = 0.35f;

        [Tooltip("Vertical offset of the blob shadow below the artifact")]
        [SerializeField] private float _shadowVerticalOffset = 0.55f;

        [Tooltip("Idle hover amplitude; reinforces the depth cue against the static shadow")]
        [SerializeField] private float _bobAmplitude = 0.08f;

        [Tooltip("Idle hover frequency in cycles per second")]
        [SerializeField] private float _bobFrequency = 1.2f;

        [Header("Spawn Placement")]
        [Tooltip("Height of spawned pickups above the platform surface")]
        [SerializeField] private float _spawnHeightOffset = 0.8f;

        [Tooltip("Horizontal scatter radius when several pickups spawn together")]
        [SerializeField] private float _spawnScatterRadius = 1.2f;

        [Header("Rolling")]
        [Tooltip("Weight multiplier applied to table entries whose bias tags match the roll context")]
        [SerializeField] private float _tagBiasMultiplier = 2f;

        [Header("Capacity Feedback (unused while the inventory is unlimited)")]
        [SerializeField] private float _rejectShakeDuration = 0.3f;

        public float PickupDuration => _pickupDuration;
        public float PickupGrowScale => _pickupGrowScale;
        public float PickupPeakFraction => _pickupPeakFraction;
        public bool PickupFadeEnabled => _pickupFadeEnabled;
        public bool PickupMoveToPlayer => _pickupMoveToPlayer;
        public int DepthLayerCount => _depthLayerCount;
        public float DepthLayerSpacing => _depthLayerSpacing;
        public float DepthLayerDarkening => _depthLayerDarkening;
        public float ShadowScale => _shadowScale;
        public float ShadowAlpha => _shadowAlpha;
        public float ShadowVerticalOffset => _shadowVerticalOffset;
        public float BobAmplitude => _bobAmplitude;
        public float BobFrequency => _bobFrequency;
        public float SpawnHeightOffset => _spawnHeightOffset;
        public float SpawnScatterRadius => _spawnScatterRadius;
        public float TagBiasMultiplier => _tagBiasMultiplier;
        public float RejectShakeDuration => _rejectShakeDuration;
    }
}
