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
        private readonly HashSet<string> _npcFillFlavors = new HashSet<string>(StringComparer.Ordinal);

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
                }
            }

            _questSites.Sort(CompareBySiteId);
            _ambientSites.Sort(CompareBySiteId);
        }

        public IReadOnlyList<SiteDefinitionData> QuestSites => _questSites;
        public IReadOnlyList<SiteDefinitionData> AmbientSites => _ambientSites;
        public IReadOnlyCollection<string> NpcFillFlavors => _npcFillFlavors;

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
