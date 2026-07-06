using System;
using System.Collections.Generic;
using UnityEngine;
using World.Dressing.Core;

namespace World.Dressing.Data
{
    /// <summary>
    /// A biome-feature dressing kit (biome-decoration brief FR1–FR3): the ground material and the
    /// feature pool one biome scatters on its platforms. Bound whole-kit from
    /// <c>BiomeAppearanceDefinition._featureKit</c>; placement/density stay on the biome config —
    /// this asset only says WHICH meshes the features are. Configuration data only, no logic.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeFeatureKit", menuName = "World/Dressing/Biome Feature Kit")]
    public class BiomeFeatureKitDefinition : DressingKitDefinition
    {
        [Serializable]
        public class FeatureEntry
        {
            [Tooltip("The feature prefab (tree / rock / cactus / tuft)")]
            [SerializeField] private GameObject _prefab;
            [Tooltip("Blocking = a tactical obstacle consuming its whole cell; decorative = visual only")]
            [SerializeField] private FeatureKind _kind = FeatureKind.SmallDecorative;
            [Tooltip("Relative draw weight among entries of the same kind; 0 = never drawn")]
            [Min(0)]
            [SerializeField] private int _weight = 1;
            [Tooltip("Uniform scale range applied per instance")]
            [Min(0.01f)]
            [SerializeField] private float _scaleMin = 0.8f;
            [Min(0.01f)]
            [SerializeField] private float _scaleMax = 1.2f;
            [Tooltip("Rough horizontal radius (world units at scale 1) kept clear of the platform " +
                     "edge; 0 = the kind's default (small 0.35 / large 0.9 / blocking 1.0)")]
            [Min(0f)]
            [SerializeField] private float _footprintOverride = 0f;
            [Tooltip("Framing style opt-in: this prop may anchor on the rim and lean past the " +
                     "walkable edge (tall trees/crags only) — everything else obeys the footprint")]
            [SerializeField] private bool _mayOverhang = false;

            public GameObject Prefab => _prefab;
            public FeatureKind Kind => _kind;
            public int Weight => _weight;
            public float ScaleMin => _scaleMin;
            public float ScaleMax => _scaleMax;
            public float FootprintOverride => _footprintOverride;
            public bool MayOverhang => _mayOverhang;
        }

        [Tooltip("Platform top material for this biome (toned at bind time); empty = keep the default ground")]
        [SerializeField] private Material _groundMaterial;
        [Tooltip("The feature pool; entry order is the planner's stable index — append, don't reorder")]
        [SerializeField] private List<FeatureEntry> _features = new List<FeatureEntry>();

        public Material GroundMaterial => _groundMaterial;
        public IReadOnlyList<FeatureEntry> Features => _features;
    }
}
