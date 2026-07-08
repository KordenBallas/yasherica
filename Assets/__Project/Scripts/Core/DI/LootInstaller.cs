using System.Collections.Generic;
using Core.Logging;
using Loot.Application;
using Loot.Core;
using Loot.Data;
using Loot.Data.Definitions;
using Loot.View;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the loot subsystem.
    /// Binds the run seed/theme providers, biome loot data, the deterministic
    /// roll service, the three acquisition-path services (platform discovery,
    /// enemy drops, quest grants) and the world pickup view factory.
    /// </summary>
    public class LootInstaller : MonoInstaller
    {
        private const string BiomeLootResourcePath = "Loot/Biomes";
        private const string LootConfigResourcePath = "Configs/LootConfig";
        private const string PickupPrefabResourcePath = "Prefabs/Loot/WorldArtifact";

        // Mask keeps the time-derived fallback seed in the same range the
        // entrypoint historically used.
        private const int TimeSeedMask = 0x0000FFFF;

        [Header("Configuration (auto-loaded from Resources when empty)")]
        [SerializeField] private LootConfig _config;

        [Header("Biome Loot (auto-loaded from Resources when empty)")]
        [SerializeField] private List<BiomeLootDefinition> _biomeLootDefinitions;

        [Header("World Pickup (auto-loaded from Resources when empty)")]
        [SerializeField] private WorldArtifactView _pickupPrefab;

        [Header("Run Seed Source")]
        [Tooltip("Entrypoint whose 'seed' field drives deterministic loot; 0 falls back to time-based")]
        [SerializeField] private AreaSceneEntrypoint _areaEntrypoint;

        public override void InstallBindings()
        {
            ResolveConfiguration();

            InstallRunContext();
            InstallData();
            InstallDomain();
            InstallApplication();
            InstallView();
        }

        private void ResolveConfiguration()
        {
            if (_config == null)
            {
                _config = Resources.Load<LootConfig>(LootConfigResourcePath);
            }

            if (_config == null)
            {
                throw new System.InvalidOperationException(
                    "[LootInstaller] LootConfig not assigned and not found at " +
                    $"Resources/{LootConfigResourcePath}.asset");
            }
        }

        // IGameLogger is intentionally NOT bound here: LoggingInstaller (installed by AreaInstaller)
        // is its single home. Zenject 6 asserts on a second creation binding for the same concrete
        // type (the singleton mark is registered before IfNotBound is evaluated), so feature
        // installers only resolve IGameLogger.

        private void InstallRunContext()
        {
            // The seed is fixed at FIRST RESOLVE (before any seeded random is created — every
            // consumer resolves the provider lazily inside FromMethod bindings) so the narrative
            // generation, reward slots, and all loot rolls share one run seed. On a continue
            // (P2-2) the seed comes from the run save instead, so every derived stream re-derives
            // identically; scenes without the persistence bindings (Arena) keep the fresh path.
            Container.Bind<IRunSeedProvider>()
                .FromMethod(ctx =>
                {
                    var seedProvider = new RunSeedProvider();
                    var restore = ctx.Container.TryResolve<Core.Persistence.RunRestoreContext>();
                    bool restoring = restore != null && restore.IsRestoring;
                    seedProvider.SetSeed(restoring ? restore.Snapshot.RunSeed : ComputeEffectiveSeed());
                    // One diagnostic line per run: geometry (platform shapes, route) is a pure
                    // function of this seed — if a resumed world looks different, compare these.
                    Debug.Log($"[LootInstaller] Run seed {seedProvider.RunSeed} " +
                              (restoring ? "(restored from run save)" : "(fresh run)"));
                    return seedProvider;
                })
                .AsSingle();

            Container.Bind<ICurrentThemeProvider>().To<CurrentThemeProvider>().AsSingle();
        }

        private void InstallData()
        {
            Container.BindInstance(_config).AsSingle();

            var biomes = LoadBiomeDefinitions();
            Container.Bind<IBiomeLootCatalog>()
                .FromMethod(ctx => new BiomeLootCatalog(biomes, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
        }

        private void InstallDomain()
        {
            Container.Bind<ILootEntryFilter>().To<PassThroughLootFilter>().AsSingle();

            // Meta-progression gate (Track R, FR3): locked artifacts vanish from every biome loot
            // table. Deps resolve lazily at first roll; a scene without the meta bindings (Arena)
            // gets a no-op filter via the TryResolve.
            Container.Bind<ILootEntryFilter>()
                .FromMethod(ctx => new MetaProgression.Integration.MetaGateLootFilter(
                    ctx.Container.Resolve<Inventory.Data.IArtifactCatalog>(),
                    ctx.Container.TryResolve<MetaProgression.Core.IMetaVocabulary>()))
                .AsSingle();

            Container.Bind<ILootRollService>()
                .FromMethod(ctx => new LootRollService(
                    ctx.Container.Resolve<IBiomeLootCatalog>(),
                    ctx.Container.Resolve<IRunSeedProvider>(),
                    ctx.Container.ResolveAll<ILootEntryFilter>(),
                    _config.TagBiasMultiplier))
                .AsSingle();

            // Quest-reward roll (P1-5): the eligible pools are projected once from the authored
            // artifact/blank catalogs into pure records, so the roller stays UnityEngine-free.
            // The projection consults the meta vocabulary (Track R) so gated tokens never roll.
            Container.Bind<QuestRewardPools>()
                .FromMethod(ctx => QuestRewardPoolsBuilder.Build(
                    ctx.Container.Resolve<Inventory.Data.IArtifactCatalog>(),
                    ctx.Container.Resolve<Mutation.Core.IPartBlankDataSource>(),
                    ctx.Container.TryResolve<MetaProgression.Core.IMetaVocabulary>()))
                .AsSingle();

            Container.Bind<IQuestRewardRoller>()
                .FromMethod(ctx => new QuestRewardRoller(
                    ctx.Container.Resolve<QuestRewardPools>(),
                    ctx.Container.Resolve<IRunSeedProvider>()))
                .AsSingle();
        }

        private void InstallApplication()
        {
            Container.Bind<IInventoryCapacityPolicy>().To<UnlimitedCapacityPolicy>().AsSingle();
            Container.Bind<IQuestRewardGranter>().To<QuestRewardGranter>().AsSingle();
            Container.Bind<IEnemyLootDropper>().To<EnemyLootDropper>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlatformLootSpawnCoordinator>().AsSingle().NonLazy();
        }

        private void InstallView()
        {
            var prefab = _pickupPrefab != null
                ? _pickupPrefab
                : Resources.Load<WorldArtifactView>(PickupPrefabResourcePath);

            if (prefab == null)
            {
                throw new System.InvalidOperationException(
                    "[LootInstaller] World artifact prefab not assigned and not found at " +
                    $"Resources/{PickupPrefabResourcePath}.prefab");
            }

            Container.BindFactory<WorldArtifactView, WorldArtifactView.Factory>()
                .FromComponentInNewPrefab(prefab);

            Container.Bind<IWorldArtifactSpawner>().To<WorldArtifactSpawner>().AsSingle();
        }

        private int ComputeEffectiveSeed()
        {
            int configuredSeed = 0;
            if (_areaEntrypoint != null)
            {
                configuredSeed = _areaEntrypoint.seed;
            }
            else
            {
                Debug.LogWarning(
                    "[LootInstaller] AreaSceneEntrypoint not assigned - run seed falls back to time-based");
            }

            return configuredSeed != 0
                ? configuredSeed
                : (int)System.DateTime.Now.Ticks & TimeSeedMask;
        }

        private List<BiomeLootDefinition> LoadBiomeDefinitions()
        {
            if (_biomeLootDefinitions != null && _biomeLootDefinitions.Count > 0)
            {
                return _biomeLootDefinitions;
            }

            var loaded = new List<BiomeLootDefinition>(
                Resources.LoadAll<BiomeLootDefinition>(BiomeLootResourcePath));
            if (loaded.Count > 0)
            {
                Debug.Log($"[LootInstaller] Auto-loaded {loaded.Count} biome loot assets " +
                          $"from Resources/{BiomeLootResourcePath}");
            }
            else
            {
                Debug.LogWarning($"[LootInstaller] No BiomeLootDefinition assets found in Inspector " +
                                 $"or Resources/{BiomeLootResourcePath}");
            }

            return loaded;
        }
    }
}
