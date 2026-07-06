using System;
using System.Collections.Generic;

namespace World.Sites.Core
{
    /// <summary>
    /// Plain list-backed <see cref="ISiteCatalog"/>. Splits the effective site records by trigger
    /// channel and orders each channel by site id so weighted picks over the lists are deterministic
    /// regardless of authoring/load order.
    /// </summary>
    public sealed class SiteCatalog : ISiteCatalog
    {
        private readonly List<SiteDefinitionData> _questSites = new List<SiteDefinitionData>();
        private readonly List<SiteDefinitionData> _ambientSites = new List<SiteDefinitionData>();
        private readonly Dictionary<string, SiteDefinitionData> _byId =
            new Dictionary<string, SiteDefinitionData>(StringComparer.Ordinal);
        // Case-insensitive: flavor tags are authored strings matched across assets (recipes, story
        // tags, enemy tags), so casing must not silently break a match.
        private readonly HashSet<string> _npcFillFlavors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _bossStoryFlavors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public SiteCatalog(IReadOnlyList<SiteDefinitionData> sites)
        {
            if (sites != null)
            {
                for (int i = 0; i < sites.Count; i++)
                {
                    var site = sites[i];
                    if (site == null || string.IsNullOrEmpty(site.SiteId) || _byId.ContainsKey(site.SiteId))
                    {
                        continue;
                    }

                    _byId.Add(site.SiteId, site);
                    (site.TriggerChannel == SiteTriggerChannel.Quest ? _questSites : _ambientSites).Add(site);
                    CollectNpcFillFlavors(site);

                    if (site.HasBossLedAnchor)
                    {
                        _bossStoryFlavors.Add(site.BossStoryFlavor);
                    }
                }
            }

            _questSites.Sort(CompareBySiteId);
            _ambientSites.Sort(CompareBySiteId);
        }

        public IReadOnlyList<SiteDefinitionData> QuestSites => _questSites;
        public IReadOnlyList<SiteDefinitionData> AmbientSites => _ambientSites;
        public IReadOnlyCollection<string> NpcFillFlavors => _npcFillFlavors;
        public IReadOnlyCollection<string> BossStoryFlavors => _bossStoryFlavors;

        public SiteDefinitionData Get(string siteId)
        {
            return !string.IsNullOrEmpty(siteId) && _byId.TryGetValue(siteId, out var site) ? site : null;
        }

        private void CollectNpcFillFlavors(SiteDefinitionData site)
        {
            for (int i = 0; i < site.FillTable.Count; i++)
            {
                var beat = site.FillTable[i].Beat;
                if (beat.Kind == ContentBaseKind.Npc && !string.IsNullOrEmpty(beat.Flavor))
                {
                    _npcFillFlavors.Add(beat.Flavor);
                }
            }
        }

        private static int CompareBySiteId(SiteDefinitionData a, SiteDefinitionData b) =>
            string.CompareOrdinal(a.SiteId, b.SiteId);
    }
}
