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

        // IGameLogger is intentionally NOT bound here: InventoryInstaller provides
        // it, and Zenject 6 asserts on a second creation binding for the same
        // concrete type (the singleton mark is registered before IfNotBound is
        // evaluated). This installer requires InventoryInstaller in the scene.

        private void InstallRunContext()
        {
            // The seed is fixed at install time so the seeded randoms created
            // during injection (narrative generation, reward slots) and all loot
            // rolls share one run seed.
            var seedProvider = new RunSeedProvider();
            seedProvider.SetSeed(ComputeEffectiveSeed());
            Container.Bind<IRunSeedProvider>().FromInstance(seedProvider).AsSingle();

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

            Container.Bind<ILootRollService>()
                .FromMethod(ctx => new LootRollService(
                    ctx.Container.Resolve<IBiomeLootCatalog>(),
                    ctx.Container.Resolve<IRunSeedProvider>(),
                    ctx.Container.ResolveAll<ILootEntryFilter>(),
                    _config.TagBiasMultiplier))
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
