using System.Collections.Generic;

namespace World.Dressing.Core
{
    /// <summary>
    /// Dictionary-backed <see cref="ISiteDressingCatalog"/> built once at install time by the kit
    /// mapper. An unknown theme id has no bound kit — base layer (brief FR8).
    /// </summary>
    public sealed class SiteDressingCatalog : ISiteDressingCatalog
    {
        private readonly Dictionary<string, SiteKitData> _kits;

        public SiteDressingCatalog(Dictionary<string, SiteKitData> kits)
        {
            _kits = kits ?? new Dictionary<string, SiteKitData>();
        }

        public bool TryGet(string dressingThemeId, out SiteKitData kit)
        {
            if (!string.IsNullOrEmpty(dressingThemeId) && _kits.TryGetValue(dressingThemeId, out kit))
            {
                return true;
            }

            kit = null;
            return false;
        }
    }
}
