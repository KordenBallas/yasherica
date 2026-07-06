using System.Collections.Generic;

namespace World.Sites.Core
{
    /// <summary>
    /// The immutable, UnityEngine-free record of one authored site, after the data layer has merged
    /// its family-default recipe with the site's own overrides (family default + per-site delta is an
    /// authoring concern; Core only ever sees the effective values). The capacity recipe is
    /// anchor beat(s) + a weighted fill table + connective Empty remainder, all in the shared
    /// <c>base·flavor</c> vocabulary — never literal content assets, so the world stays procedural.
    /// </summary>
    public sealed class SiteDefinitionData
    {
        public SiteDefinitionData(
            string siteId,
            string familyId,
            int footprintMin,
            int footprintMax,
            int triggerWeight,
            IReadOnlyList<ContentBeat> anchorBeats,
            int fillBudgetMin,
            int fillBudgetMax,
            IReadOnlyList<WeightedBeat> fillTable,
            string dressingThemeId,
            string bossStoryFlavor = null,
            int bossCrewMin = 0,
            int bossCrewMax = 0)
        {
            SiteId = siteId ?? string.Empty;
            FamilyId = familyId ?? string.Empty;
            FootprintMin = footprintMin < 1 ? 1 : footprintMin;
            FootprintMax = footprintMax < FootprintMin ? FootprintMin : footprintMax;
            TriggerWeight = triggerWeight < 0 ? 0 : triggerWeight;
            AnchorBeats = anchorBeats ?? System.Array.Empty<ContentBeat>();
            FillBudgetMin = fillBudgetMin < 0 ? 0 : fillBudgetMin;
            FillBudgetMax = fillBudgetMax < FillBudgetMin ? FillBudgetMin : fillBudgetMax;
            FillTable = fillTable ?? System.Array.Empty<WeightedBeat>();

            DressingThemeId = dressingThemeId ?? string.Empty;
            TriggerChannel = AnchorBeats.Count > 0 && AnchorBeats[0].Kind == ContentBaseKind.Npc
                ? SiteTriggerChannel.Quest
                : SiteTriggerChannel.Ambient;

            BossStoryFlavor = bossStoryFlavor ?? string.Empty;
            BossCrewMin = bossCrewMin < 0 ? 0 : bossCrewMin;
            BossCrewMax = bossCrewMax < BossCrewMin ? BossCrewMin : bossCrewMax;
        }

        public string SiteId { get; }
        public string FamilyId { get; }

        /// <summary>Rolled per instance: how many contiguous platforms the block reserves.</summary>
        public int FootprintMin { get; }
        public int FootprintMax { get; }

        /// <summary>Relative weight among sites of the same trigger channel when a trigger lands.</summary>
        public int TriggerWeight { get; }

        /// <summary>The guaranteed beat(s) that are the site's reason to exist; emitted first.</summary>
        public IReadOnlyList<ContentBeat> AnchorBeats { get; }

        /// <summary>Rolled per instance: how many non-anchor platforms carry a secondary beat.</summary>
        public int FillBudgetMin { get; }
        public int FillBudgetMax { get; }

        /// <summary>The weighted table the fill budget draws from; footprint beyond it stays Empty.</summary>
        public IReadOnlyList<WeightedBeat> FillTable { get; }

        /// <summary>The dressing-theme key stamped onto every block platform (M5 seam).</summary>
        public string DressingThemeId { get; }

        /// <summary>Derived from the first anchor's base kind — see <see cref="SiteTriggerChannel"/>.</summary>
        public SiteTriggerChannel TriggerChannel { get; }

        /// <summary>
        /// The story flavor a boss-led site's leader is cast from (e.g. "bandit-boss"); empty when the
        /// site is not boss-led. On a boss-led site the Combat anchor becomes boss + crew: a talkable
        /// or hostile boss NPC fronting a crew that joins his one fight (bandit-camp brief).
        /// </summary>
        public string BossStoryFlavor { get; }

        /// <summary>Rolled per instance: how many crew enemies stand behind the boss.</summary>
        public int BossCrewMin { get; }
        public int BossCrewMax { get; }

        /// <summary>True when the site's Combat anchor is a boss-led camp (a boss flavor is authored).</summary>
        public bool HasBossLedAnchor => BossStoryFlavor.Length > 0;
    }
}
