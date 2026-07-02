using System.Collections.Generic;

namespace World.Sites.Core
{
    /// <summary>
    /// The run's authored site vocabulary, split by trigger channel. Built by the data layer from the
    /// site/family assets at install time; an empty catalog means no sites are authored and the world
    /// allocation path behaves exactly as before (pure Wild).
    /// </summary>
    public interface ISiteCatalog
    {
        /// <summary>Quest-channel sites (NPC-anchored settlements), ordered by site id (deterministic).</summary>
        IReadOnlyList<SiteDefinitionData> QuestSites { get; }

        /// <summary>Ambient-channel sites (combat/loot-anchored), ordered by site id (deterministic).</summary>
        IReadOnlyList<SiteDefinitionData> AmbientSites { get; }

        /// <summary>
        /// The union of NPC fill flavors appearing in any site's fill table (e.g. "townsfolk").
        /// Stories tagged with one of these are ambient colour: the planner must exclude them from
        /// quest picks so chatter never satisfies the rare quest slot.
        /// </summary>
        IReadOnlyCollection<string> NpcFillFlavors { get; }

        /// <summary>The site with this id, or null when unknown.</summary>
        SiteDefinitionData Get(string siteId);
    }
}
