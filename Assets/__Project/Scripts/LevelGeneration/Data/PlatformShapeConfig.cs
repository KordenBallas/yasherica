using System;
using Combat.Battlefield;
using UnityEngine;

namespace LevelGeneration.Data
{
    /// <summary>
    /// ScriptableObject platform size/shape dials (the platform-hex-surface-and-shape brief).
    /// Configuration data only — generation consumes the mapped Core
    /// <c>LevelGeneration.Surface.PlatformShapeSettings</c>, never this SO. One asset governs the hex
    /// tiling (shared with the combat grid — the single source of truth for cell size/orientation),
    /// the per-content-kind size/shape profiles, the battlefield minimum, and the decorative rim.
    /// </summary>
    [CreateAssetMenu(fileName = "PlatformShapeConfig", menuName = "Level Generation/Platform Shape Config")]
    public class PlatformShapeConfig : ScriptableObject
    {
        [Serializable]
        public class ShapeProfileData
        {
            [Tooltip("Minimum whole hex cells for this content kind")]
            [Min(1)]
            [SerializeField] private int _minCells = 2;
            [Tooltip("Maximum whole hex cells (raised to min if lower)")]
            [Min(1)]
            [SerializeField] private int _maxCells = 4;
            [Tooltip("0 = ragged/organic growth, 8 = tight round blob")]
            [Range(0, 8)]
            [SerializeField] private int _compactness = 3;

            public ShapeProfileData() { }

            public ShapeProfileData(int minCells, int maxCells, int compactness)
            {
                _minCells = minCells;
                _maxCells = maxCells;
                _compactness = compactness;
            }

            public int MinCells => _minCells;
            public int MaxCells => _maxCells;
            public int Compactness => _compactness;
        }

        [Header("Hex Tiling (shared with the combat grid)")]
        [Tooltip("Hex cell size in world units; the combat grid derives from the same value. " +
                 "Gotcha: Prefabs/HexagonOutline is authored for size 2")]
        [Min(0.1f)]
        [SerializeField] private float _hexCellSize = 2f;
        [Tooltip("Hex orientation; the combat grid derives from the same value")]
        [SerializeField] private HexOrientation _hexOrientation = HexOrientation.Flat;

        [Header("Size/Shape Profiles (per content kind)")]
        [Tooltip("Empty/traversal platforms — a quick step, not an arena")]
        [SerializeField] private ShapeProfileData _emptyProfile = new ShapeProfileData(2, 4, 3);
        [Tooltip("Loot-only platforms — visibly small")]
        [SerializeField] private ShapeProfileData _lootProfile = new ShapeProfileData(3, 5, 3);
        [Tooltip("Combat-capable platforms — sized to fight on (see battlefield minimum)")]
        [SerializeField] private ShapeProfileData _combatProfile = new ShapeProfileData(12, 18, 6);
        [Tooltip("NPC platforms with no forced fight")]
        [SerializeField] private ShapeProfileData _npcProfile = new ShapeProfileData(4, 7, 3);

        [Header("Battlefield")]
        [Tooltip("Whole-cell floor every combat-capable platform must meet, regardless of profile")]
        [Min(1)]
        [SerializeField] private int _battlefieldMinimumCells = 12;

        [Header("Decorative Rim (the organic island silhouette)")]
        [Tooltip("Base outward width of the non-walkable rim, world units")]
        [Min(0f)]
        [SerializeField] private float _rimWidth = 1.2f;
        [Tooltip("Per-vertex rim irregularity, percent of the rim width")]
        [Range(0, 100)]
        [SerializeField] private int _rimJitterPercent = 35;
        [Tooltip("How far the rim droops below the walkable top, world units")]
        [Min(0f)]
        [SerializeField] private float _rimDropHeight = 0.4f;

        [Header("Body & Layout (moved off AreaGeneratorConfig)")]
        [Tooltip("Platform thickness (extrusion below the top), world units")]
        [Min(0.1f)]
        [SerializeField] private float _platformThickness = 1f;
        [Tooltip("Gap between neighboring platforms, world units")]
        [Min(0f)]
        [SerializeField] private float _gapBetweenPlatforms = 2f;
        [Tooltip("Max height deviation between consecutive platforms, world units")]
        [Min(0f)]
        [SerializeField] private float _heightDeviation = 1.5f;

        [Header("Muted Traversal Tiling")]
        [Tooltip("Per-cell top inset feeding the bevel seams; 0 = perfectly flat (tiling invisible)")]
        [Min(0f)]
        [SerializeField] private float _cellInset = 0.06f;

        public float HexCellSize => _hexCellSize;
        public HexOrientation Orientation => _hexOrientation;
        public ShapeProfileData EmptyProfile => _emptyProfile;
        public ShapeProfileData LootProfile => _lootProfile;
        public ShapeProfileData CombatProfile => _combatProfile;
        public ShapeProfileData NpcProfile => _npcProfile;
        public int BattlefieldMinimumCells => _battlefieldMinimumCells;
        public float RimWidth => _rimWidth;
        public int RimJitterPercent => _rimJitterPercent;
        public float RimDropHeight => _rimDropHeight;
        public float PlatformThickness => _platformThickness;
        public float GapBetweenPlatforms => _gapBetweenPlatforms;
        public float HeightDeviation => _heightDeviation;
        public float CellInset => _cellInset;
    }
}
