using System.Collections.Generic;
using Combat.Battlefield;
using Core.Logging;
using LevelGeneration.Surface;
using Loot.Core;
using Narrative.Director.Core;
using World.Sites.Core;

namespace World.Dressing.Core
{
    /// <summary>
    /// Routes a platform to the right dressing planner: a site-block platform is dressed by its
    /// site's kit (site dressing replaces biome features there — one voice per place, KISS), a
    /// wild platform by the active biome's feature kit. Owns the seed streams: biome features ride
    /// a private per-platform stream (order-independent across windows, like the shape stream);
    /// site dressing derives block-shared draws from the instance id and per-platform jitter from
    /// instance + index — so a restored run replays the same dressing from the persisted stamps.
    /// Pure C#; fail-safe by construction (no kit → the empty plan, warned once per key).
    /// </summary>
    public sealed class EnvironmentDressingPlanner : IEnvironmentDressingPlanner
    {
        private const string BiomeSeedContext = "biome-features";
        private const string SiteSeedContext = "site-dressing";

        private readonly IBiomeFeaturePoolCatalog _biomeCatalog;
        private readonly ISiteDressingCatalog _siteCatalog;
        private readonly ICurrentThemeProvider _themeProvider;
        private readonly IRunSeedProvider _seedProvider;
        private readonly IGameLogger _logger;
        private readonly BiomeFeaturePlanner _biomePlanner = new BiomeFeaturePlanner();
        private readonly SiteDressingPlanner _sitePlanner = new SiteDressingPlanner();
        private readonly HashSet<string> _warnedKeys = new HashSet<string>();

        public EnvironmentDressingPlanner(
            IBiomeFeaturePoolCatalog biomeCatalog,
            ISiteDressingCatalog siteCatalog,
            ICurrentThemeProvider themeProvider,
            IRunSeedProvider seedProvider,
            IGameLogger logger = null)
        {
            _biomeCatalog = biomeCatalog;
            _siteCatalog = siteCatalog;
            _themeProvider = themeProvider;
            _seedProvider = seedProvider;
            _logger = logger;
        }

        public PlatformDressingPlan Plan(
            int nodeId,
            SiteStamp site,
            PlatformContentKind kind,
            PlatformHexSurface surface,
            int battlefieldMinCells)
        {
            if (surface == null)
            {
                return PlatformDressingPlan.Empty;
            }

            return site.IsWild
                ? PlanBiomeFeatures(nodeId, kind, surface, battlefieldMinCells)
                : PlanSiteDressing(site, surface, battlefieldMinCells);
        }

        private PlatformDressingPlan PlanBiomeFeatures(
            int nodeId, PlatformContentKind kind, PlatformHexSurface surface, int battlefieldMinCells)
        {
            var theme = _themeProvider.CurrentTheme;
            if (_biomeCatalog == null || !_biomeCatalog.TryGet(theme, out var pool, out var density))
            {
                WarnOnce($"biome:{theme}",
                    $"[EnvironmentDressingPlanner] No feature kit bound for biome '{theme}' — base layer only.");
                return PlatformDressingPlan.Empty;
            }

            var rng = DeriveRng($"{BiomeSeedContext}:{nodeId}");
            return _biomePlanner.Plan(surface, pool, density, theme, battlefieldMinCells, rng);
        }

        private PlatformDressingPlan PlanSiteDressing(
            SiteStamp site, PlatformHexSurface surface, int battlefieldMinCells)
        {
            if (_siteCatalog == null || string.IsNullOrEmpty(site.DressingThemeId)
                || !_siteCatalog.TryGet(site.DressingThemeId, out var kit))
            {
                WarnOnce($"site:{site.DressingThemeId}",
                    $"[EnvironmentDressingPlanner] No dressing kit for theme id '{site.DressingThemeId}' " +
                    $"(site '{site.SiteId}') — base layer only.");
                return PlatformDressingPlan.Empty;
            }

            var instanceRng = DeriveRng($"{SiteSeedContext}:{site.InstanceId}");
            var platformRng = DeriveRng($"{SiteSeedContext}:{site.InstanceId}:{site.Index}");
            return _sitePlanner.Plan(
                surface, site, kit, _themeProvider.CurrentTheme, battlefieldMinCells,
                instanceRng, platformRng);
        }

        private IRandomSource DeriveRng(string context)
        {
            int runSeed = _seedProvider?.RunSeed ?? 0;
            return new DeterministicRandom(unchecked((ulong)LootSeed.Derive(runSeed, context)));
        }

        private void WarnOnce(string key, string message)
        {
            if (_warnedKeys.Add(key))
            {
                _logger?.Warning(LogCategory.LevelGeneration, message);
            }
        }
    }
}
