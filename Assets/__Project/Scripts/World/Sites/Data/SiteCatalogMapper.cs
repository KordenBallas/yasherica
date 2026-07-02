using System.Collections.Generic;
using Core.Logging;
using World.Sites.Core;

namespace World.Sites.Data
{
    /// <summary>
    /// The only bridge from the authored <see cref="SiteDefinition"/>/<see cref="SiteFamilyDefinition"/>
    /// assets to the UnityEngine-free <see cref="SiteCatalog"/> (CLAUDE.md §7). Merges each site's
    /// family default with its explicit overrides into one effective <see cref="SiteDefinitionData"/>,
    /// and validates: a site without an id, or without any effective anchor beat (no family and no
    /// override — it would have no reason to exist), is skipped with a warning.
    /// </summary>
    public static class SiteCatalogMapper
    {
        public static SiteCatalog ToCatalog(IReadOnlyList<SiteDefinition> sites, IGameLogger logger = null)
        {
            var records = new List<SiteDefinitionData>();
            if (sites == null)
            {
                return new SiteCatalog(records);
            }

            var seenIds = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < sites.Count; i++)
            {
                var site = sites[i];
                if (site == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(site.SiteId))
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[SiteCatalogMapper] Site asset '{site.name}' has no siteId - skipped.");
                    continue;
                }

                if (!seenIds.Add(site.SiteId))
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[SiteCatalogMapper] Duplicate siteId '{site.SiteId}' ('{site.name}') - skipped.");
                    continue;
                }

                var family = site.Family;
                var anchors = MapBeats(site.OverrideAnchorBeats
                    ? site.AnchorBeats
                    : family != null ? family.DefaultAnchorBeats : null);
                if (anchors.Count == 0)
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[SiteCatalogMapper] Site '{site.SiteId}' has no anchor beat (no family default " +
                        "and no override) - a site needs a reason to exist; skipped.");
                    continue;
                }

                int fillMin = site.OverrideFillBudget ? site.FillBudgetMin
                    : family != null ? family.DefaultFillBudgetMin : 0;
                int fillMax = site.OverrideFillBudget ? site.FillBudgetMax
                    : family != null ? family.DefaultFillBudgetMax : 0;
                var fillTable = MapWeighted(site.OverrideFillTable
                    ? site.FillTable
                    : family != null ? family.DefaultFillTable : null);

                records.Add(new SiteDefinitionData(
                    site.SiteId,
                    family != null ? family.FamilyId : string.Empty,
                    site.FootprintMin,
                    site.FootprintMax,
                    site.TriggerWeight,
                    anchors,
                    fillMin,
                    fillMax,
                    fillTable,
                    site.DressingThemeId));
            }

            return new SiteCatalog(records);
        }

        private static IReadOnlyList<ContentBeat> MapBeats(IReadOnlyList<ContentBeatEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return System.Array.Empty<ContentBeat>();
            }

            var beats = new List<ContentBeat>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                {
                    beats.Add(new ContentBeat(entries[i].Kind, entries[i].Flavor));
                }
            }

            return beats;
        }

        private static IReadOnlyList<WeightedBeat> MapWeighted(IReadOnlyList<WeightedBeatEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return System.Array.Empty<WeightedBeat>();
            }

            var rows = new List<WeightedBeat>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                {
                    rows.Add(new WeightedBeat(new ContentBeat(entries[i].Kind, entries[i].Flavor), entries[i].Weight));
                }
            }

            return rows;
        }
    }
}
