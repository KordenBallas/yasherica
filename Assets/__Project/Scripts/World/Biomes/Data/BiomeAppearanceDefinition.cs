using System.Collections.Generic;
using LevelGeneration;
using LevelGeneration.Route;
using UnityEngine;
using World.Dressing.Data;

namespace World.Biomes.Data
{
    /// <summary>
    /// ScriptableObject mapping one level theme (biome) to its landscape character (the
    /// world-backdrop-and-elevation brief): routed-path corridor and curve dials, feature-arc and
    /// routing-landmark dials, elevation tier set, backdrop silhouette/sky character, and the
    /// biome's dressing binding (environment-dressing: whole-kit feature reference + tone key +
    /// placement density — the kit says which meshes, this seam owns density and seeding).
    /// Contains ONLY configuration data — NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeAppearanceDefinition", menuName = "World/Biome Appearance")]
    public class BiomeAppearanceDefinition : ScriptableObject
    {
        [Header("Biome")]
        [SerializeField] private LevelTheme _theme;

        [Header("Routed Path (lateral weave, bounded corridor)")]
        [Tooltip("Max lateral (depth) excursion of the route from the run axis, world units")]
        [Min(0.1f)]
        [SerializeField] private float _corridorHalfWidth = BiomeLandscapeSettings.DefaultCorridorHalfWidth;
        [Tooltip("Forward distance of one baseline bend, world units — several platforms per bend")]
        [Min(1f)]
        [SerializeField] private float _baselineWavelength = BiomeLandscapeSettings.DefaultBaselineWavelength;
        [Tooltip("Lateral amplitude of the baseline wander (auto-capped so arcs still fit the corridor)")]
        [Min(0f)]
        [SerializeField] private float _baselineAmplitude = BiomeLandscapeSettings.DefaultBaselineAmplitude;

        [Header("Feature Arcs (routing around a landmark)")]
        [Tooltip("Forward length of one arc slot; at most one feature arc occurs per slot")]
        [Min(1f)]
        [SerializeField] private float _arcSlotLength = BiomeLandscapeSettings.DefaultArcSlotLength;
        [Tooltip("Chance that a slot carries a feature arc")]
        [Range(0, 100)]
        [SerializeField] private int _arcChancePercent = BiomeLandscapeSettings.DefaultArcChancePercent;
        [Tooltip("Max extra toward-camera bulge of a feature arc, world units")]
        [Min(0f)]
        [SerializeField] private float _arcDepth = BiomeLandscapeSettings.DefaultArcDepth;
        [Tooltip("Min arc half-length along the run, world units")]
        [Min(1f)]
        [SerializeField] private float _arcHalfLengthMin = BiomeLandscapeSettings.DefaultArcHalfLengthMin;
        [Tooltip("Max arc half-length along the run, world units (auto-capped to fit its slot)")]
        [Min(1f)]
        [SerializeField] private float _arcHalfLengthMax = BiomeLandscapeSettings.DefaultArcHalfLengthMax;

        [Header("Routing Landmarks (midground, off-platform, never walkable)")]
        [Tooltip("How far behind the path (away from the camera) a landmark sits, world units")]
        [Min(0f)]
        [SerializeField] private float _landmarkOffset = BiomeLandscapeSettings.DefaultLandmarkOffset;
        [Tooltip("Min uniform landmark scale")]
        [Min(0.1f)]
        [SerializeField] private float _landmarkScaleMin = BiomeLandscapeSettings.DefaultLandmarkScaleMin;
        [Tooltip("Max uniform landmark scale")]
        [Min(0.1f)]
        [SerializeField] private float _landmarkScaleMax = BiomeLandscapeSettings.DefaultLandmarkScaleMax;
        [Tooltip("Landmark prefabs (peak / dune / boulder / tree-clump). Empty = procedural placeholder silhouette")]
        [SerializeField] private List<GameObject> _landmarkKit = new List<GameObject>();
        [Tooltip("Muted tint applied to placeholder landmark silhouettes")]
        [SerializeField] private Color _landmarkTint = new Color(0.47f, 0.52f, 0.47f);

        [Header("Elevation Tiers (visual only — read, never traversal)")]
        [Tooltip("Number of deliberate height levels")]
        [Min(1)]
        [SerializeField] private int _tierCount = BiomeLandscapeSettings.DefaultTierCount;
        [Tooltip("Vertical distance between adjacent tiers, world units")]
        [Min(0f)]
        [SerializeField] private float _tierStep = BiomeLandscapeSettings.DefaultTierStep;
        [Tooltip("Forward distance of one full elevation swell, world units")]
        [Min(1f)]
        [SerializeField] private float _tierWavelength = BiomeLandscapeSettings.DefaultTierWavelength;

        [Header("World Backdrop (distant hazed horizon, lowest in the focus hierarchy)")]
        [Tooltip("Silhouette meshes for the far ridge layers. Empty = procedural placeholder ridge")]
        [SerializeField] private List<Mesh> _silhouetteKit = new List<Mesh>();
        [Tooltip("Peak height of the backdrop ridge line, world units")]
        [Min(0f)]
        [SerializeField] private float _backdropRidgeAmplitude = BiomeLandscapeSettings.DefaultBackdropRidgeAmplitude;
        [Tooltip("Forward distance of one backdrop ridge undulation, world units")]
        [Min(1f)]
        [SerializeField] private float _backdropRidgeWavelength = BiomeLandscapeSettings.DefaultBackdropRidgeWavelength;
        [Tooltip("Sky band color at the top")]
        [SerializeField] private Color _skyTopTint = new Color(0.62f, 0.71f, 0.76f);
        [Tooltip("Sky band color at the horizon")]
        [SerializeField] private Color _skyHorizonTint = new Color(0.82f, 0.85f, 0.84f);
        [Tooltip("Haze color the far ridge layers fade toward")]
        [SerializeField] private Color _hazeTint = new Color(0.75f, 0.79f, 0.79f);

        [Header("Dressing (environment-dressing: kit binding + tone + density)")]
        [Tooltip("The biome's feature kit, bound whole-kit (swap = repoint this one field). Empty = base layer only")]
        [SerializeField] private BiomeFeatureKitDefinition _featureKit;
        [Tooltip("Muted biome key the bind-time tone treatment lerps kit materials toward")]
        [SerializeField] private Color _toneTint = new Color(0.55f, 0.57f, 0.52f);
        [Tooltip("How far kit materials are pulled toward the tone tint (0 = native, 1 = flat key)")]
        [Range(0f, 1f)]
        [SerializeField] private float _toneStrength = 0.45f;
        [Tooltip("Blocking obstacles per 100 surface cells — sparse and deliberate")]
        [Min(0f)]
        [SerializeField] private float _blockersPer100Cells = 4f;
        [Tooltip("Decorative clusters (copse / outcrop / tuft patch) per 100 surface cells")]
        [Min(0f)]
        [SerializeField] private float _decorClustersPer100Cells = 10f;
        [Tooltip("Protected movement-lane half-width, in cell (hex-size) units")]
        [Min(0f)]
        [SerializeField] private float _laneHalfWidthCells = 1.1f;
        [Tooltip("The biome's world-backdrop kit (distant scatter horizon), bound whole-kit. " +
                 "Empty = no scatter (the procedural ridge strips stay the only horizon)")]
        [SerializeField] private BackdropKitDefinition _backdropKit;
        [Tooltip("Distant-scatter density: items per 100 forward world units — a horizon, not a crowd")]
        [Min(0f)]
        [SerializeField] private float _backdropScatterPer100Units = 4f;

        public LevelTheme Theme => _theme;
        public float CorridorHalfWidth => _corridorHalfWidth;
        public float BaselineWavelength => _baselineWavelength;
        public float BaselineAmplitude => _baselineAmplitude;
        public float ArcSlotLength => _arcSlotLength;
        public int ArcChancePercent => _arcChancePercent;
        public float ArcDepth => _arcDepth;
        public float ArcHalfLengthMin => _arcHalfLengthMin;
        public float ArcHalfLengthMax => _arcHalfLengthMax;
        public float LandmarkOffset => _landmarkOffset;
        public float LandmarkScaleMin => _landmarkScaleMin;
        public float LandmarkScaleMax => _landmarkScaleMax;
        public IReadOnlyList<GameObject> LandmarkKit => _landmarkKit;
        public Color LandmarkTint => _landmarkTint;
        public int TierCount => _tierCount;
        public float TierStep => _tierStep;
        public float TierWavelength => _tierWavelength;
        public IReadOnlyList<Mesh> SilhouetteKit => _silhouetteKit;
        public float BackdropRidgeAmplitude => _backdropRidgeAmplitude;
        public float BackdropRidgeWavelength => _backdropRidgeWavelength;
        public Color SkyTopTint => _skyTopTint;
        public Color SkyHorizonTint => _skyHorizonTint;
        public Color HazeTint => _hazeTint;
        public BiomeFeatureKitDefinition FeatureKit => _featureKit;
        public Color ToneTint => _toneTint;
        public float ToneStrength => _toneStrength;
        public float BlockersPer100Cells => _blockersPer100Cells;
        public float DecorClustersPer100Cells => _decorClustersPer100Cells;
        public float LaneHalfWidthCells => _laneHalfWidthCells;
        public BackdropKitDefinition BackdropKit => _backdropKit;
        public float BackdropScatterPer100Units => _backdropScatterPer100Units;
    }
}
