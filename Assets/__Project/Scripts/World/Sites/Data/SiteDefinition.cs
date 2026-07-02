using System.Collections.Generic;
using UnityEngine;

namespace World.Sites.Data
{
    /// <summary>
    /// One authored site (Camp / Village / City / Ruin / Lair / …). Configuration data only — the
    /// planner consumes the mapped Core <c>SiteDefinitionData</c>, never this SO. Recipe attributes
    /// inherit the referenced family's defaults unless their explicit override toggle is on, so a new
    /// optional attribute added later defaults to "inherit" and every existing asset stays valid
    /// (world-sites brief R7/R10).
    /// </summary>
    [CreateAssetMenu(fileName = "Site", menuName = "World/Sites/Site Definition")]
    public class SiteDefinition : ScriptableObject
    {
        [Tooltip("Stable site id (e.g. city) — referenced by site:<id> story tags and dressing")]
        [SerializeField] private string _siteId = string.Empty;
        [Tooltip("Designer-facing name; not used by the engine")]
        [SerializeField] private string _displayName = string.Empty;
        [Tooltip("The family whose default recipe this site inherits")]
        [SerializeField] private SiteFamilyDefinition _family;

        [Header("Footprint & Trigger")]
        [Tooltip("Contiguous platform-count range the site reserves (city ~4-5, village ~2-3, camp ~1-2)")]
        [Min(1)]
        [SerializeField] private int _footprintMin = 1;
        [Min(1)]
        [SerializeField] private int _footprintMax = 2;
        [Tooltip("Relative weight among same-channel sites when a trigger lands; 0 = never rolled (site: tag only)")]
        [Min(0)]
        [SerializeField] private int _triggerWeight = 1;

        [Header("Dressing (M5 seam)")]
        [Tooltip("Dressing-theme key stamped onto every block platform; inert until the site-dressing pass")]
        [SerializeField] private string _dressingThemeId = string.Empty;

        [Header("Recipe Overrides (off = inherit the family default)")]
        [SerializeField] private bool _overrideAnchorBeats;
        [SerializeField] private List<ContentBeatEntry> _anchorBeats = new List<ContentBeatEntry>();
        [SerializeField] private bool _overrideFillBudget;
        [Min(0)]
        [SerializeField] private int _fillBudgetMin;
        [Min(0)]
        [SerializeField] private int _fillBudgetMax = 1;
        [SerializeField] private bool _overrideFillTable;
        [SerializeField] private List<WeightedBeatEntry> _fillTable = new List<WeightedBeatEntry>();

        public string SiteId => _siteId;
        public string DisplayName => _displayName;
        public SiteFamilyDefinition Family => _family;
        public int FootprintMin => _footprintMin;
        public int FootprintMax => _footprintMax;
        public int TriggerWeight => _triggerWeight;
        public string DressingThemeId => _dressingThemeId;
        public bool OverrideAnchorBeats => _overrideAnchorBeats;
        public IReadOnlyList<ContentBeatEntry> AnchorBeats => _anchorBeats;
        public bool OverrideFillBudget => _overrideFillBudget;
        public int FillBudgetMin => _fillBudgetMin;
        public int FillBudgetMax => _fillBudgetMax;
        public bool OverrideFillTable => _overrideFillTable;
        public IReadOnlyList<WeightedBeatEntry> FillTable => _fillTable;
    }
}
