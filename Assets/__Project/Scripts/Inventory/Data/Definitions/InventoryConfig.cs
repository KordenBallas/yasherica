using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Data.Definitions
{
    /// <summary>
    /// All inventory and crafting tunables in one place.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "Config/Inventory Config")]
    public class InventoryConfig : ScriptableObject
    {
        [Header("Crafting")]
        [Tooltip("How many staged artifacts trigger a combine attempt")]
        [SerializeField, Min(2)] private int _itemsToCombine = 2;

        [Header("Fusion (emergent grammar tunables)")]
        [Tooltip("Tier bonus per trait that appears in two or more combined inputs (amplify)")]
        [SerializeField, Min(0)] private int _amplifyTierBonus = 1;
        [Tooltip("Score weight per trait shared between the fusion target and a candidate artifact")]
        [SerializeField, Min(0f)] private float _traitOverlapWeight = 1f;
        [Tooltip("Score penalty per candidate trait absent from the fusion target")]
        [SerializeField, Min(0f)] private float _traitMismatchWeight = 0.25f;
        [Tooltip("Score penalty per point of tier distance between target and candidate")]
        [SerializeField, Min(0f)] private float _tierProximityWeight = 0.5f;

        [Header("Starting Inventory (dev seed until a loot system exists)")]
        [SerializeField] private List<ArtifactDefinition> _startingInventory;

        [Header("Bubble Layout")]
        [SerializeField, Min(0.01f)] private float _minBubbleRadius = 0.06f;
        [SerializeField, Min(0.01f)] private float _maxBubbleRadius = 0.18f;
        [Tooltip("How quickly bubbles shrink as the item count grows")]
        [SerializeField, Min(0f)] private float _radiusFalloff = 0.35f;
        [Tooltip("Clearance between bubbles and the pot interior edge")]
        [SerializeField, Min(0f)] private float _edgePadding = 0.03f;

        [Header("Bubble Drift")]
        [Tooltip("Peak per-axis displacement of an idle bubble from its base point")]
        [SerializeField, Min(0f)] private float _bubbleDriftAmplitude = 0.01f;
        [Tooltip("Drift speed in cycles per second")]
        [SerializeField, Min(0f)] private float _bubbleDriftFrequency = 0.35f;

        [Header("Pseudo-3D Artifacts")]
        [Tooltip("Total stacked sprite quads per artifact, front layer included")]
        [SerializeField, Min(1)] private int _artifactLayerCount = 4;
        [Tooltip("Distance between consecutive layers along the quad's depth axis")]
        [SerializeField, Min(0f)] private float _artifactLayerSpacing = 0.015f;
        [Tooltip("Brightness multiplier the rearmost layer fades to")]
        [SerializeField, Range(0f, 1f)] private float _artifactBackLayerDarkening = 0.45f;

        [Header("Liquid Boil")]
        [Tooltip("Peak vertical displacement of the liquid surface")]
        [SerializeField, Min(0f)] private float _liquidWaveAmplitude = 0.02f;
        [Tooltip("Boil speed in oscillations per second")]
        [SerializeField, Min(0f)] private float _liquidWaveFrequency = 1.2f;
        [Tooltip("How tightly the waves ripple across the surface (higher = denser)")]
        [SerializeField, Min(0f)] private float _liquidWaveSpatialScale = 6f;
        [Tooltip("Blend of a second crossed wave that breaks up the primary pattern")]
        [SerializeField, Min(0f)] private float _liquidSecondaryWaveWeight = 0.5f;
        [Tooltip("Speed of the emission glow pulse, in pulses per second")]
        [SerializeField, Min(0f)] private float _liquidEmissionPulseSpeed = 1.5f;
        [Tooltip("How strongly the emission glow swells and fades")]
        [SerializeField, Range(0f, 1f)] private float _liquidEmissionPulseStrength = 0.3f;

        [Header("Character Facing")]
        [Tooltip("Time the character takes to turn toward the camera and back")]
        [SerializeField, Min(0f)] private float _facingRotationDuration = 0.4f;

        [Header("Timings")]
        [Tooltip("Camera blend time when zooming to/from the belly pot")]
        [SerializeField, Min(0f)] private float _cameraTransitionTime = 1.0f;
        [Tooltip("Travel time of staged artifacts merging toward the result anchor")]
        [SerializeField, Min(0f)] private float _mergeDuration = 0.45f;
        [Tooltip("Scale-in time of the crafted result popping above the pot")]
        [SerializeField, Min(0f)] private float _resultPopDuration = 0.35f;
        [Tooltip("Travel time of a collected result dropping into the pot")]
        [SerializeField, Min(0f)] private float _resultDropDuration = 0.4f;

        public int ItemsToCombine => _itemsToCombine;
        public int AmplifyTierBonus => _amplifyTierBonus;
        public float TraitOverlapWeight => _traitOverlapWeight;
        public float TraitMismatchWeight => _traitMismatchWeight;
        public float TierProximityWeight => _tierProximityWeight;
        public IReadOnlyList<ArtifactDefinition> StartingInventory => _startingInventory;
        public float MinBubbleRadius => _minBubbleRadius;
        public float MaxBubbleRadius => _maxBubbleRadius;
        public float RadiusFalloff => _radiusFalloff;
        public float EdgePadding => _edgePadding;
        public float BubbleDriftAmplitude => _bubbleDriftAmplitude;
        public float BubbleDriftFrequency => _bubbleDriftFrequency;
        public int ArtifactLayerCount => _artifactLayerCount;
        public float ArtifactLayerSpacing => _artifactLayerSpacing;
        public float ArtifactBackLayerDarkening => _artifactBackLayerDarkening;
        public float LiquidWaveAmplitude => _liquidWaveAmplitude;
        public float LiquidWaveFrequency => _liquidWaveFrequency;
        public float LiquidWaveSpatialScale => _liquidWaveSpatialScale;
        public float LiquidSecondaryWaveWeight => _liquidSecondaryWaveWeight;
        public float LiquidEmissionPulseSpeed => _liquidEmissionPulseSpeed;
        public float LiquidEmissionPulseStrength => _liquidEmissionPulseStrength;
        public float FacingRotationDuration => _facingRotationDuration;
        public float CameraTransitionTime => _cameraTransitionTime;
        public float MergeDuration => _mergeDuration;
        public float ResultPopDuration => _resultPopDuration;
        public float ResultDropDuration => _resultDropDuration;
    }
}
