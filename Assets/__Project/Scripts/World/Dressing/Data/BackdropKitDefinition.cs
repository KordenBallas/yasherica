using System;
using System.Collections.Generic;
using UnityEngine;

namespace World.Dressing.Data
{
    /// <summary>
    /// A world-backdrop dressing kit (world-backdrop-fill brief, Track E4): the distant-scatter
    /// meshes a biome's horizon is built from (dunes/mesas behind the desert, hills/tree-clumps
    /// behind the forest). Bound whole-kit from <c>BiomeAppearanceDefinition._backdropKit</c>;
    /// placement, density, and seeding stay on the consuming seam (the scatter planner) — the kit
    /// only says WHICH silhouettes exist. The third kit kind of the E1 dressing contract; the
    /// binding/swap/quarantine/tone rules apply unchanged. Configuration data only, no logic.
    /// </summary>
    [CreateAssetMenu(fileName = "BackdropKit", menuName = "World/Dressing/Backdrop Kit")]
    public class BackdropKitDefinition : DressingKitDefinition
    {
        [Serializable]
        public class ScatterEntry
        {
            [Tooltip("The distant-scatter prefab (mesa / dune / hill / tree-clump / rock formation)")]
            [SerializeField] private GameObject _prefab;
            [Tooltip("Relative draw weight; 0 = never drawn")]
            [Min(0)]
            [SerializeField] private int _weight = 1;
            [Tooltip("Uniform scale range — backdrop silhouettes are typically scaled well up")]
            [Min(0.01f)]
            [SerializeField] private float _scaleMin = 3f;
            [Min(0.01f)]
            [SerializeField] private float _scaleMax = 6f;

            public GameObject Prefab => _prefab;
            public int Weight => _weight;
            public float ScaleMin => _scaleMin;
            public float ScaleMax => _scaleMax;
        }

        [Tooltip("The scatter pool; entry order is the planner's stable index — append, don't reorder")]
        [SerializeField] private List<ScatterEntry> _entries = new List<ScatterEntry>();

        public IReadOnlyList<ScatterEntry> Entries => _entries;
    }
}
