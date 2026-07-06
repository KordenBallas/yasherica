namespace World.Dressing.Core
{
    /// <summary>
    /// Pure lookup from a site's dressing-theme id (<c>SiteStamp.DressingThemeId</c>) to its bound
    /// kit's shape. An unknown/empty id returns false — the base-layer fail-safe (brief FR8).
    /// </summary>
    public interface ISiteDressingCatalog
    {
        bool TryGet(string dressingThemeId, out SiteKitData kit);
    }
}
