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

        [Header("Brew Layout (stable spots)")]
        [Tooltip("Fixed radius of every brew bubble (stable spots need a stable size)")]
        [SerializeField, Min(0.01f)] private float _brewBubbleRadius = 0.07f;
        [Tooltip("Extra clearance factor between spot centres (perspective safety)")]
        [SerializeField, Min(1f)] private float _brewSpotSpacingMargin = 1.15f;
        [Tooltip("Clearance between bubbles and the bowl's inner wall")]
        [SerializeField, Min(0f)] private float _edgePadding = 0.02f;
        [Tooltip("Clearance between the lowest bubbles and the bowl floor")]
        [SerializeField, Min(0f)] private float _brewFloorClearance = 0.01f;
        [Tooltip("Max depth offset per spot so the stack reads as a volume")]
        [SerializeField, Min(0f)] private float _brewDepthJitter = 0.05f;
        [Tooltip("Extra spot layers above the rim for an overfilled pot")]
        [SerializeField, Min(0)] private int _brewOverflowLayers = 2;

        [Header("Brew Event Physics (view juice)")]
        [Tooltip("Height above its spot a dropped-in bubble falls from")]
        [SerializeField, Min(0f)] private float _bubbleDropInHeight = 0.35f;
        [Tooltip("Fall time of a dropped-in bubble")]
        [SerializeField, Min(0f)] private float _bubbleDropInDuration = 0.3f;
        [Tooltip("Reach of the splash nudge around a drop-in")]
        [SerializeField, Min(0f)] private float _splashRadius = 0.26f;
        [Tooltip("Peak velocity kick a splash gives the nearest neighbour")]
        [SerializeField, Min(0f)] private float _splashImpulse = 0.35f;
        [Tooltip("Velocity kick neighbours get when a bubble leaves the brew")]
        [SerializeField, Min(0f)] private float _removalBobImpulse = 0.12f;
        [Tooltip("Spring stiffness pulling nudged bubbles back to their spots")]
        [SerializeField, Min(0f)] private float _settleStiffness = 30f;
        [Tooltip("Spring damping so the settle dies out instead of ringing")]
        [SerializeField, Min(0f)] private float _settleDamping = 9f;

        [Header("Liquid Fullness")]
        [Tooltip("Waterline height (bowl-local) of an empty pot — never bone-dry")]
        [SerializeField, Min(0.01f)] private float _liquidMinFillHeight = 0.18f;
        [Tooltip("Waterline height (bowl-local) of a brimming pot — never overflowing")]
        [SerializeField, Min(0.01f)] private float _liquidMaxFillHeight = 0.58f;
        [Tooltip("Artifact count at which the pot reads as full")]
        [SerializeField, Min(1)] private int _artifactsAtFullPot = 18;
        [Tooltip("Clearance kept between the topmost bubble and the waterline")]
        [SerializeField, Min(0f)] private float _liquidFillHeadroom = 0.05f;

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
        public float BrewBubbleRadius => _brewBubbleRadius;
        public float BrewSpotSpacingMargin => _brewSpotSpacingMargin;
        public float EdgePadding => _edgePadding;
        public float BrewFloorClearance => _brewFloorClearance;
        public float BrewDepthJitter => _brewDepthJitter;
        public int BrewOverflowLayers => _brewOverflowLayers;
        public float BubbleDropInHeight => _bubbleDropInHeight;
        public float BubbleDropInDuration => _bubbleDropInDuration;
        public float SplashRadius => _splashRadius;
        public float SplashImpulse => _splashImpulse;
        public float RemovalBobImpulse => _removalBobImpulse;
        public float SettleStiffness => _settleStiffness;
        public float SettleDamping => _settleDamping;
        public float LiquidMinFillHeight => _liquidMinFillHeight;
        public float LiquidMaxFillHeight => _liquidMaxFillHeight;
        public int ArtifactsAtFullPot => _artifactsAtFullPot;
        public float LiquidFillHeadroom => _liquidFillHeadroom;
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
