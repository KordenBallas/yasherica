using System.Collections.Generic;
using Character;
using CharacterProgression.Core;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Combat.Data.Providers;
using Combat.Core;
using Combat.Execution;
using Combat.Integration;
using Combat.TurnManagement;
using Combat.View;
using Core.Camera;
using Core.Logging;
using LevelGeneration;
using Platform;
using UnityEngine;
using World.Biomes;
using World.Biomes.Data;
using World.Races.Core;
using World.Races.Data;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Unified Zenject installer for the Area scene.
    /// Combines game core, platform system, and combat system bindings.
    /// </summary>
    public class AreaInstaller : MonoInstaller
    {
        /// <summary>The default ability queue depth (the third <see cref="CombatConfig"/> argument).</summary>
        private const int MaxAbilityQueueSizeDefault = 3;

        private const string BiomeAppearanceResourcePath = "World/Biomes";
        private const string BiomeProgressionResourcePath = "World/Biomes/BiomeProgressionConfig";
        private const string RacesResourcePath = "World/Races";
        /// <summary>Kit assets load recursively from here, so Demo/ (the quarantine) rides in.</summary>
        private const string DressingKitsResourcePath = "World/Dressing";
        /// <summary>Seed-context for the journey's own random stream (kept apart from the narrative-slice stream).</summary>
        private const string BiomeJourneySeedContext = "biome-journey";

        [Header("Configuration ScriptableObjects")]
        [SerializeField] private CombatMovementConfig _movementConfig;
        [SerializeField] private InputConfig _inputConfig;
        [SerializeField] private HexDirectionConfig _hexDirectionConfig;
        [Tooltip("Platform size/shape + hex tiling dials; auto-loads from " +
                 "Resources/LevelGeneration/PlatformShapeConfig when unset")]
        [SerializeField] private LevelGeneration.Data.PlatformShapeConfig _platformShapeConfig;
        [Tooltip("Per-biome landscape appearance (routed path / tiers / backdrop); auto-loads from " +
                 "Resources/World/Biomes when empty. An unauthored biome uses code defaults")]
        [SerializeField] private List<BiomeAppearanceDefinition> _biomeAppearances;
        [Tooltip("Biome rotation for the run (tier / weight / stretch per biome); auto-loads from " +
                 "Resources/World/Biomes/BiomeProgressionConfig when unset. Missing = fixed Forest")]
        [SerializeField] private BiomeProgressionConfig _biomeProgressionConfig;
        [Tooltip("The race roster (races-passport.md); auto-loads from Resources/World/Races when " +
                 "empty. Missing = a raceless world (every part reads kindless)")]
        [SerializeField] private List<RaceDefinition> _raceDefinitions;

        /// <summary>The biome appearance list InstallWorldBiomeBindings resolved (inspector or
        /// Resources) — the dressing catalogs map the same assets, so both read one binding.</summary>
        private List<BiomeAppearanceDefinition> _resolvedBiomeAppearances;

        [Header("Data Definitions (Optional - for data-driven system)")]
        [Tooltip("Status effect definitions for the data-driven system")]
        [SerializeField] private List<StatusEffectDefinition> _statusEffectDefinitions;

        [Tooltip("Ability definitions for the data-driven system")]
        [SerializeField] private List<AbilityDefinition> _abilityDefinitions;

        [Tooltip("Enemy definitions for the data-driven system")]
        [SerializeField] private List<EnemyDefinition> _enemyDefinitions;

        [Tooltip("Hero definition for the player character")]
        [SerializeField] private HeroDefinition _heroDefinition;

        public override void InstallBindings()
        {
            // Single logging home for the scene; feature installers only resolve IGameLogger and must
            // never bind it (re-binding UnityGameLogger AsSingle trips Zenject 6's "AsSingle multiple
            // times" assert, even with IfNotBound).
            LoggingInstaller.Install(Container);

            // Save-file stores + the run's persistence services (P2-2 save/continue).
            PersistenceInstaller.Install(Container);
            InstallPersistenceBindings();

            // Scene navigation: the death return rides RunLifecycleService → Hub (O1).
            Container.Bind<Core.SceneFlow.ISceneLoader>().To<Core.SceneFlow.SceneLoader>().AsSingle();

            InstallGameCoreBindings();
            InstallWorldBiomeBindings();
            InstallDressingBindings();
            InstallRaceBindings();
            InstallPlatformBindings();
            InstallCombatBindings();

            // NPC proximity interaction (F-prompt talk, aggro-on-approach, intent markers, name labels).
            NpcInteractionInstaller.Install(Container);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Developer state overlay (quests + director facts). Editor/dev-build only; never ships.
            DevToolsInstaller.Install(Container);
#endif
        }

        private void InstallPersistenceBindings()
        {
            // The pending-restore decision: lazy, so the run save file is read exactly once, at the
            // first consumer (the seed provider, during container build).
            Container.Bind<Core.Persistence.RunRestoreContext>()
                .FromMethod(ctx => new Core.Persistence.RunRestoreContext(
                    ctx.Container.Resolve<Core.Persistence.IRunSaveStore>().TryLoad(out var snapshot)
                        ? snapshot
                        : null))
                .AsSingle();

            // How this run starts (O1): restore wins (biome from the save), a fresh boot consumes
            // the Hub's one-shot run-setup file, nothing on disk = defaults (bare hero, seeded pick).
            Container.Bind<Core.Persistence.RunStartConditions>()
                .FromMethod(ctx => Core.Persistence.RunStartConditions.Resolve(
                    ctx.Container.Resolve<Core.Persistence.RunRestoreContext>(),
                    ctx.Container.Resolve<Core.Persistence.IRunSetupStore>()))
                .AsSingle();

            // World memory into the fact store at boot; Meta-partition flush service for the
            // savepoint/death writers. Both resolve narrative-slice bindings (IFactStore,
            // IFactKeyRegistry) installed on this same SceneContext.
            Container.BindInterfacesTo<Core.Persistence.MetaMemoryBootstrap>().AsSingle();
            Container.Bind<Core.Persistence.IMetaMemoryFlush>()
                .To<Core.Persistence.MetaMemoryFlushService>()
                .AsSingle();

            // The whole-run capture/restore aggregate and the world persistence bridge.
            Container.Bind<Core.Persistence.IRunStateService>()
                .To<Core.Persistence.RunStateService>()
                .AsSingle();
            Container.BindInterfacesAndSelfTo<LevelGeneration.WorldStatePersistenceBridge>().AsSingle();

            // The hero-body save seam lives here, not in CharacterSystemInstaller: it needs the
            // scene's hero visual eagerly (IInitializable), which only the Area scene guarantees.
            Container.BindInterfacesAndSelfTo<CharacterSystem.Runtime.HeroBodyRestorer>().AsSingle();

            // The tasted-forms catalog writer (P4-5): every part the hero carries becomes a
            // Meta-horizon fact the Arena draft board reads. Same eager-hero-visual constraint
            // as HeroBodyRestorer, hence Area-bound.
            Container.BindInterfacesAndSelfTo<CharacterSystem.Integration.TastedFormsRecorder>().AsSingle();

            // The Hub's starting-part install (O1): fresh runs only, applied once the rig assembles.
            // Same eager-hero-visual constraint as HeroBodyRestorer, hence Area-bound.
            Container.BindInterfacesAndSelfTo<CharacterSystem.Integration.StartingPartApplier>().AsSingle();

            // The savepoint writer (platform entries, D7) and the death hook (Defeat → meta flush →
            // run-save delete, FR2). The dialogue state feeds in as a delegate so the autosave
            // stays pure C# testable without the whole runner graph.
            Container.BindInterfacesAndSelfTo<Core.Persistence.AutosaveService>()
                .FromMethod(ctx =>
                {
                    var runner = ctx.Container.Resolve<Narrative.Dialogue.DialogueRunner>();
                    return new Core.Persistence.AutosaveService(
                        ctx.Container.Resolve<Core.Persistence.IRunStateService>(),
                        ctx.Container.Resolve<Core.Persistence.IRunSaveStore>(),
                        ctx.Container.Resolve<Core.Persistence.IMetaMemoryFlush>(),
                        () => runner.State,
                        ctx.Container.Resolve<Core.Logging.IGameLogger>());
                })
                .AsSingle();
            Container.BindInterfacesTo<Core.Persistence.RunLifecycleService>().AsSingle();

            // Graceful-exit savepoint: quitting (or stopping play mode) saves the current
            // platform's progress instead of falling back to the entry-time save.
            Container.BindInterfacesTo<Core.Persistence.QuitSavepointHook>().AsSingle();

            // Restore ordering (D6/D9): run restore first, then the always-on world memory on top,
            // then everything else (including the entrypoint's world generation) at default order.
            Container.BindInterfacesTo<Core.Persistence.RunRestoreCoordinator>().AsSingle();
            Container.BindExecutionOrder<Core.Persistence.RunRestoreCoordinator>(-200);
            Container.BindExecutionOrder<Core.Persistence.MetaMemoryBootstrap>(-100);

            // The run counter ticks on top of the loaded memory (fresh boots only), before the
            // entrypoint plans window 0 — spine soft floors read it (D7, P3-1).
            Container.BindInterfacesTo<Core.Persistence.RunCounterService>().AsSingle();
            Container.BindExecutionOrder<Core.Persistence.RunCounterService>(-90);
        }

        private void InstallGameCoreBindings()
        {
            // (The legacy one-shot scenario/graph generators were removed; the streaming director
            // builds platforms per window via RunStreamingCoordinator.)

            // Per-run progression record: read + write surfaces share one instance.
            // Same lifetime as the run seed (scene singleton); cross-scene/session
            // persistence is the backlog "Run-state persistence/save-load" item.
            Container.Bind(typeof(IRunProgressionRecord), typeof(IRunProgressionRecorder))
                .To<RunProgressionRecord>()
                .AsSingle();
            Container.Bind<RunConditionEvaluator>().AsSingle();

            // Factory Registry (legacy - kept for backwards compatibility)
            Container.Bind<IPlatformFactoryRegistry>().To<PlatformFactory>().AsSingle();

            // Camera Service
            Container.Bind<ICameraService>().To<CameraService>().FromComponentInHierarchy().AsSingle();

            // Camera Configuration from Resources. The asset lives under Configs/ — the binding was
            // latently wrong ("CameraConfig") and never fired until the world backdrop began
            // resolving CameraConfig for the isometric yaw.
            Container.Bind<CameraConfig>().FromResource("Configs/CameraConfig").AsSingle();

            // Scene entrypoints - bind to IInitializable so Zenject calls Initialize() after injection
            Container.BindInterfacesTo<AreaSceneEntrypoint>().FromComponentInHierarchy().AsSingle();
        }

        private void InstallWorldBiomeBindings()
        {
            // Biome landscape appearance (routed path / elevation tiers / backdrop character).
            // Missing assets are fine — the route model falls back to code defaults per theme.
            var appearances = _biomeAppearances;
            if (appearances == null || appearances.Count == 0)
            {
                appearances = new List<BiomeAppearanceDefinition>(
                    Resources.LoadAll<BiomeAppearanceDefinition>(BiomeAppearanceResourcePath));
                if (appearances.Count == 0)
                {
                    Debug.LogWarning("[AreaInstaller] No BiomeAppearanceDefinition assets found in " +
                                     $"Inspector or Resources/{BiomeAppearanceResourcePath} — " +
                                     "landscape uses code defaults for every biome.");
                }
            }

            _resolvedBiomeAppearances = appearances;
            var captured = appearances;
            Container.Bind<IBiomeAppearanceCatalog>()
                .FromMethod(ctx => new BiomeAppearanceCatalog(captured, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            // Biome journey (biome-selection-along-the-run): tier/weight/stretch roster mapped to the
            // pure settings record; the journey itself rides its OWN seeded stream so stretch draws
            // never perturb the shared narrative-slice stream (and vice versa).
            if (_biomeProgressionConfig == null)
            {
                _biomeProgressionConfig = Resources.Load<BiomeProgressionConfig>(BiomeProgressionResourcePath);
                if (_biomeProgressionConfig == null)
                {
                    Debug.LogWarning("[AreaInstaller] BiomeProgressionConfig not assigned and not found at " +
                                     $"Resources/{BiomeProgressionResourcePath} — the run stays in fixed Forest.");
                }
            }

            var progressionConfig = _biomeProgressionConfig;
            Container.Bind<LevelGeneration.Journey.BiomeProgressionSettings>()
                .FromMethod(ctx => BiomeProgressionConfigMapper.ToSettings(
                    progressionConfig, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<LevelGeneration.Journey.IBiomeJourney>()
                .FromMethod(ctx =>
                {
                    int runSeed = ctx.Container.Resolve<Loot.Core.IRunSeedProvider>().RunSeed;
                    var random = new Narrative.Director.Core.DeterministicRandom(
                        unchecked((ulong)Loot.Core.LootSeed.Derive(runSeed, BiomeJourneySeedContext)));

                    // The Hub-chosen entry homeland (O1) overrides only the window-0 pick; the
                    // climb stays on the journey's own seeded stream.
                    var conditions = ctx.Container.Resolve<Core.Persistence.RunStartConditions>();
                    LevelGeneration.LevelTheme? startingTheme =
                        conditions.TryGetStartingTheme(out var chosen)
                            ? chosen
                            : (LevelGeneration.LevelTheme?)null;

                    return new LevelGeneration.Journey.BiomeJourney(
                        ctx.Container.Resolve<LevelGeneration.Journey.BiomeProgressionSettings>(),
                        random,
                        ctx.Container.Resolve<IGameLogger>(),
                        startingTheme);
                })
                .AsSingle();
        }

        private void InstallDressingBindings()
        {
            // Environment dressing (dressing-kit contract): kit assets are the ONLY reachable home
            // of store-pack references; the biome appearance assets bind feature kits whole-kit.
            // No kits authored = every surface renders its base layer — never a failure.
            var siteKits = new List<World.Dressing.Data.SiteDressingKitDefinition>(
                Resources.LoadAll<World.Dressing.Data.SiteDressingKitDefinition>(DressingKitsResourcePath));
            var biomeKits = new List<World.Dressing.Data.BiomeFeatureKitDefinition>(
                Resources.LoadAll<World.Dressing.Data.BiomeFeatureKitDefinition>(DressingKitsResourcePath));

            var appearances = _resolvedBiomeAppearances;
            Container.Bind<World.Dressing.Core.IBiomeFeaturePoolCatalog>()
                .FromMethod(ctx => World.Dressing.Data.DressingKitMapper.ToBiomeCatalog(
                    appearances, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<World.Dressing.Core.ISiteDressingCatalog>()
                .FromMethod(ctx => World.Dressing.Data.DressingKitMapper.ToSiteCatalog(
                    siteKits, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<World.Dressing.Core.IEnvironmentDressingPlanner>()
                .To<World.Dressing.Core.EnvironmentDressingPlanner>()
                .AsSingle();

            Container.Bind<World.Dressing.Data.DressingKitLibrary>()
                .FromMethod(ctx => new World.Dressing.Data.DressingKitLibrary(
                    biomeKits, siteKits, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
            Container.Bind<World.Dressing.View.ToneMaterialCache>().AsSingle();
            Container.Bind<IEnvironmentDressingSpawner>()
                .To<World.Dressing.View.EnvironmentDressingSpawner>()
                .AsSingle();
        }

        private void InstallRaceBindings()
        {
            // The race roster (races-passport.md): one RaceDefinition asset per race, mapped to the
            // UnityEngine-free IRaceRoster. An empty roster is a valid (raceless) world.
            var races = _raceDefinitions;
            if (races == null || races.Count == 0)
            {
                races = new List<RaceDefinition>(Resources.LoadAll<RaceDefinition>(RacesResourcePath));
            }

            var captured = races;
            Container.Bind<IRaceRoster>()
                .FromMethod(ctx => RaceRosterMapper.ToRoster(captured, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
        }

        private void InstallPlatformBindings()
        {
            // State Factory (content-driven state selection)
            Container.Bind<IPlatformStateFactory>()
                .To<PlatformStateFactory>()
                .AsSingle();

            // Platform Factory (uses state factory)
            Container.BindFactory<int, Platform.Platform, Platform.Platform.Factory>();

            // Default state factories
            Container.BindFactory<PlatformActiveState, PlatformActiveState.Factory>();
            Container.BindFactory<PlatformIdleState, PlatformIdleState.Factory>();
            Container.BindFactory<PlatformCompletedState, PlatformCompletedState.Factory>();
            Container.BindFactory<PlatformLockedState, PlatformLockedState.Factory>();

            // Placeholder state factories for future content types
            Container.BindFactory<DialogueActiveState, DialogueActiveState.Factory>();
            Container.BindFactory<CutsceneActiveState, CutsceneActiveState.Factory>();
        }

        private void InstallCombatBindings()
        {
            ValidateCombatConfigurations();
            InstallCombatConfigurations();
            InstallCharacterAndInputBindings();
            InstallAnimationBindings();
            InstallAbilityAndStatusEffectBindings();
            InstallDataProviderBindings();
            InstallBattlefieldBindings();
            InstallCoreSystemBindings();
            InstallTurnManagementBindings();
            InstallCombatControllerBindings();
            InstallCombatStateFactories();
        }

        private void ValidateCombatConfigurations()
        {
            if (_movementConfig == null)
            {
                throw new System.InvalidOperationException(
                    "[AreaInstaller] CombatMovementConfig not assigned! " +
                    "Assign the config asset in the scene's AreaInstaller component. " +
                    "The asset should be located in Resources/CombatMovementConfig.asset");
            }

            if (_inputConfig == null)
            {
                throw new System.InvalidOperationException(
                    "[AreaInstaller] InputConfig not assigned! " +
                    "Assign the config asset in the scene's AreaInstaller component. " +
                    "The asset should be located in Resources/InputConfig.asset");
            }

            if (_hexDirectionConfig == null)
            {
                throw new System.InvalidOperationException(
                    "[AreaInstaller] HexDirectionConfig not assigned! " +
                    "Assign the config asset in the scene's AreaInstaller component. " +
                    "The asset should be located in Resources/HexDirectionConfig.asset");
            }
        }

        private void InstallCombatConfigurations()
        {
            Container.BindInstance(_movementConfig).AsSingle();
            Container.BindInstance(_inputConfig).AsSingle();
            Container.BindInstance(_hexDirectionConfig).AsSingle();

            // Platform size/shape settings (the platform-hex brief): the one source of truth for the
            // hex tiling — the surface generation consumes it directly and CombatConfig is built FROM
            // it below, so the ground and the combat grid can never disagree on cell size/orientation.
            if (_platformShapeConfig == null)
            {
                _platformShapeConfig = Resources.Load<LevelGeneration.Data.PlatformShapeConfig>(
                    "LevelGeneration/PlatformShapeConfig");
                if (_platformShapeConfig == null)
                {
                    Debug.LogWarning("[AreaInstaller] PlatformShapeConfig not assigned and not found at " +
                                     "Resources/LevelGeneration/PlatformShapeConfig — using code defaults.");
                }
            }

            var shapeSettings = LevelGeneration.Data.PlatformShapeConfigMapper.ToSettings(_platformShapeConfig);
            Container.Bind<LevelGeneration.Surface.PlatformShapeSettings>()
                .FromInstance(shapeSettings)
                .AsSingle();

            Container.Bind<CombatConfig>().AsSingle().WithArguments(
                shapeSettings.HexSize,
                shapeSettings.Orientation,
                MaxAbilityQueueSizeDefault
            );
        }

        private void InstallCharacterAndInputBindings()
        {
            // Character Registry
            Container.Bind<ICharacterRegistry>()
                .To<CharacterRegistry>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();

            // Player Registry
            Container.Bind<IPlayerRegistry>()
                .To<PlayerRegistry>()
                .AsSingle();

            // Input Controller (platform-specific)
            // TODO: Add platform detection logic to bind correct implementation
            Container.Bind<Combat.Input.IInputController>()
                .To<Combat.Input.PCInputController>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();

            // Combat Action Panel View (optional - from scene hierarchy)
            Container.Bind<ICombatActionPanelView>()
                .FromComponentInHierarchy()
                .AsCached()
                .IfNotBound();
        }

        private void InstallAnimationBindings()
        {
            Container.Bind<ICharacterMovementAnimator>()
                .To<SimpleLerpAnimator>()
                .AsSingle();

            Container.Bind<CombatEntryAnimator>().AsSingle();

            // Services
            Container.Bind<CharacterCombatInitializer>().AsSingle();
        }

        private void InstallAbilityAndStatusEffectBindings()
        {
            // Status Effect Factory (bind first as abilities may need it)
            Container.Bind<IStatusEffectFactory>()
                .To<StatusEffectFactory>()
                .AsSingle();

            // Ability Factory
            Container.Bind<IAbilityFactory>()
                .To<AbilityFactory>()
                .AsSingle();

            // Resolves part-granted abilities (active + passive) from the player's equipped
            // parts. Depends on IPartCatalog, bound by CharacterSystemInstaller in the same context.
            Container.Bind<Combat.Integration.IPartAbilityResolver>()
                .To<Combat.Integration.PartAbilityResolver>()
                .AsSingle();

            // Status Effect Trigger Processor
            Container.Bind<StatusEffectTriggerProcessor>()
                .AsSingle();
        }

        private void InstallDataProviderBindings()
        {
            // Hero Definition
            // Auto-load from Resources if not manually assigned
            HeroDefinition heroDef = _heroDefinition;
            if (heroDef == null)
            {
                heroDef = Resources.Load<HeroDefinition>("Heroes/HeroDefinition");
                if (heroDef != null)
                {
                    Debug.Log("[AreaInstaller] Auto-loaded hero definition from Resources/Heroes/HeroDefinition");
                }
                else
                {
                    Debug.LogWarning("[AreaInstaller] No hero definition found in Inspector or Resources");
                }
            }

            if (heroDef != null)
            {
                Container.BindInstance(heroDef).AsSingle();
            }

            // Ability Data Provider (optional - for data-driven system)
            if (_abilityDefinitions != null && _abilityDefinitions.Count > 0)
            {
                Container.Bind<IAbilityDataProvider>()
                    .To<ScriptableObjectAbilityProvider>()
                    .AsSingle()
                    .WithArguments(_abilityDefinitions as IReadOnlyList<AbilityDefinition>);
            }

            // Enemy combat integration
            // Auto-load enemy definitions from Resources if not manually assigned. Both authoring
            // locations are loaded (combat enemies + narrative-slice story enemies) so every enemy the
            // planner can place — story combat slots and ambient biome-pool monsters alike — resolves
            // in the combat data provider. Deduped by enemy id (first location wins).
            List<EnemyDefinition> enemyDefs = _enemyDefinitions;
            if (enemyDefs == null || enemyDefs.Count == 0)
            {
                enemyDefs = new List<EnemyDefinition>();
                var seenIds = new HashSet<int>();
                foreach (var folder in new[] { "Enemies/Definitions", "Narrative/Enemies" })
                {
                    foreach (var enemy in Resources.LoadAll<EnemyDefinition>(folder))
                    {
                        if (enemy != null && seenIds.Add(enemy.EnemyId))
                        {
                            enemyDefs.Add(enemy);
                        }
                    }
                }

                if (enemyDefs.Count > 0)
                {
                    Debug.Log($"[AreaInstaller] Auto-loaded {enemyDefs.Count} enemy definitions from Resources (Enemies/Definitions + Narrative/Enemies)");
                }
                else
                {
                    Debug.LogWarning("[AreaInstaller] No enemy definitions found in Inspector or Resources");
                }
            }

            // Use ScriptableObject provider if definitions are available, otherwise fallback to simple provider
            if (enemyDefs != null && enemyDefs.Count > 0)
            {
                Container.Bind<Combat.Data.IEnemyDataProvider>()
                    .To<ScriptableObjectEnemyDataProvider>()
                    .AsSingle()
                    .WithArguments(enemyDefs as IReadOnlyList<EnemyDefinition>);
            }
            else
            {
                Container.Bind<Combat.Data.IEnemyDataProvider>()
                    .To<Combat.Data.SimpleEnemyDataProvider>()
                    .AsSingle();
            }

            Container.Bind<EnemyCombatIntegrator>()
                .AsSingle();

            // Single spawn path for enemy bodies (shared humanoid assembly first, prefab/capsule
            // fallback), used by both platform combat states.
            Container.Bind<Platform.EnemyVisualSpawner>()
                .AsSingle();

            // Enemy round controller: paces the Resolve phase (committed intents fire one by one)
            Container.Bind<Combat.Player.EnemyRoundController>()
                .AsSingle();
        }

        private void InstallBattlefieldBindings()
        {
            // The battlefield builds its SurfaceHexGrid directly from the platform's hex surface;
            // the legacy per-orientation grid factories are gone with the scan grids.
            Container.BindFactory<IBattlefield, BattlefieldFactory>().To<Combat.Battlefield.Battlefield>();
        }

        private void InstallCoreSystemBindings()
        {
            // Combat activity flag: combat layer mutates the concrete tracker,
            // other subsystems (e.g. inventory) observe ICombatActivityTracker.
            Container.BindInterfacesAndSelfTo<CombatActivityTracker>().AsSingle();

            Container.Bind<IDamageSystem>().To<DamageSystem>().AsSingle();
            Container.Bind<IAbilityShapeCalculator>().To<AbilityShapeCalculator>().AsSingle();
            // D3: the live ability-animation cue fan-in — the executor notifies it, the animation view reads it.
            Container.Bind<IAbilityFiredSink>().To<AbilityFiredSink>().AsSingle();
            Container.Bind<IAbilityExecutor>().To<AbilityExecutor>().AsSingle();
            Container.Bind<IActionExecutor>().To<ActionExecutor>().AsSingle();
            Container.Bind<IActionValidator>().To<ActionValidator>().AsSingle();

            // Telegraph support: outcome preview (ghost data), ability-definition lookup
            // (icon source), and the unit-visual registry presentation reads.
            Container.Bind<Combat.Execution.IAbilityOutcomeCalculator>()
                .To<Combat.Execution.AbilityOutcomeCalculator>().AsSingle();
            Container.Bind<Combat.Data.Providers.IAbilityDefinitionCatalog>()
                .To<Combat.Data.Providers.AbilityDefinitionCatalog>().AsSingle();
            Container.Bind<Combat.View.ICombatUnitViewRegistry>()
                .To<Combat.View.CombatUnitViewRegistry>().AsSingle();

            // Battlefield integration
            Container.Bind<CombatBattlefield>().AsSingle();

            // Rules
            Container.Bind<Combat.Rules.MovementRules>().AsSingle();
        }

        private void InstallTurnManagementBindings()
        {
            Container.Bind<ITurnManager>().To<TurnManager>().AsSingle();

            // Plan → Act → Resolve round: enemies commit intents up front (planner) and the
            // committed intents fire verbatim at Resolve (resolver).
            Container.Bind<Combat.TurnManagement.EnemyIntentPlanner>().AsSingle();
            Container.Bind<Combat.Execution.EnemyIntentResolver>().AsSingle();
        }

        private void InstallCombatControllerBindings()
        {
            Container.Bind<ICombatController>()
                .To<CombatController>()
                .AsSingle();

            Container.Bind<IFactory<ICombatController>>()
                .To<CombatControllerFactory>()
                .AsSingle();

            // Per-fight controllers fan their outcomes into one scene-scoped relay (the P2-2 death
            // hook listens here — a direct subscription would miss the factory-made controllers).
            Container.Bind<ICombatOutcomeRelay>().To<CombatOutcomeRelay>().AsSingle();
        }

        private void InstallCombatStateFactories()
        {
            Container.BindFactory<CombatActiveState, CombatActiveState.Factory>();
            Container.BindFactory<CombatIdleState, CombatIdleState.Factory>();
        }
    }
}
