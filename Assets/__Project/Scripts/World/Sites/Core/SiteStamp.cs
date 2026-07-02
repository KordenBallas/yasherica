namespace World.Sites.Core
{
    /// <summary>
    /// The site membership a platform slot carries: which authored site it belongs to, which run-unique
    /// site instance, and where in the block it sits. Default = Wild (no site). This is the data seam
    /// the M5 site-dressing pass reads to make a cluster read as one place (shared kit, gate placement,
    /// density gradient); this change only threads it through to the graph node.
    /// </summary>
    public readonly struct SiteStamp
    {
        public SiteStamp(string siteId, int instanceId, int index, int footprint, string dressingThemeId)
        {
            SiteId = siteId ?? string.Empty;
            InstanceId = instanceId;
            Index = index;
            Footprint = footprint;
            DressingThemeId = dressingThemeId ?? string.Empty;
        }

        /// <summary>The authored site's id (e.g. "city"); empty for a Wild platform.</summary>
        public string SiteId { get; }

        /// <summary>Run-unique instance counter so two villages in one run are distinct places.</summary>
        public int InstanceId { get; }

        /// <summary>This platform's position within the block, 0 = the anchor.</summary>
        public int Index { get; }

        /// <summary>The block's platform count (this instance's rolled footprint).</summary>
        public int Footprint { get; }

        /// <summary>The site's dressing-theme key (inert until the M5 site-dressing pass).</summary>
        public string DressingThemeId { get; }

        /// <summary>No site — the Wild baseline every slot carries unless a block claimed it.</summary>
        public static SiteStamp Wild => default;

        public bool IsWild => string.IsNullOrEmpty(SiteId);
    }
}
