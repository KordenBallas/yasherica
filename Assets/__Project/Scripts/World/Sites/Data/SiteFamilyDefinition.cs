using System.Collections.Generic;
using UnityEngine;

namespace World.Sites.Data
{
    /// <summary>
    /// The family-default capacity recipe a site inherits (Settlement / Landmark). Configuration data
    /// only — sites reference one family and override only their delta, so revising a family default
    /// shifts every site of that family in one edit (world-sites brief R7/R11).
    /// </summary>
    [CreateAssetMenu(fileName = "SiteFamily", menuName = "World/Sites/Site Family")]
    public class SiteFamilyDefinition : ScriptableObject
    {
        [Tooltip("Stable family id (e.g. settlement, landmark)")]
        [SerializeField] private string _familyId = string.Empty;

        [Header("Default Capacity Recipe")]
        [Tooltip("Anchor beat(s) — the content that is a site's reason to exist; emitted first. The FIRST anchor's kind decides the trigger channel (NPC = quest roll, Combat/Loot = ambient roll)")]
        [SerializeField] private List<ContentBeatEntry> _defaultAnchorBeats = new List<ContentBeatEntry>();
        [Tooltip("How many of the remaining footprint platforms carry a secondary beat (rolled per instance)")]
        [Min(0)]
        [SerializeField] private int _defaultFillBudgetMin;
        [Min(0)]
        [SerializeField] private int _defaultFillBudgetMax = 1;
        [Tooltip("The weighted table fill draws come from; footprint beyond anchors+fill stays connective Empty. Never list corpse-loot — it is the outcome of a Combat beat")]
        [SerializeField] private List<WeightedBeatEntry> _defaultFillTable = new List<WeightedBeatEntry>();

        [Header("Default Boss Anchor (boss-led camp; empty flavor = not boss-led)")]
        [Tooltip("Story flavor the planner casts the boss from; empty = the Combat anchor stays a plain fight")]
        [SerializeField] private string _defaultBossStoryFlavor = string.Empty;
        [Min(0)]
        [SerializeField] private int _defaultBossCrewMin;
        [Min(0)]
        [SerializeField] private int _defaultBossCrewMax;

        public string FamilyId => _familyId;
        public IReadOnlyList<ContentBeatEntry> DefaultAnchorBeats => _defaultAnchorBeats;
        public int DefaultFillBudgetMin => _defaultFillBudgetMin;
        public int DefaultFillBudgetMax => _defaultFillBudgetMax;
        public IReadOnlyList<WeightedBeatEntry> DefaultFillTable => _defaultFillTable;
        public string DefaultBossStoryFlavor => _defaultBossStoryFlavor;
        public int DefaultBossCrewMin => _defaultBossCrewMin;
        public int DefaultBossCrewMax => _defaultBossCrewMax;
    }
}
