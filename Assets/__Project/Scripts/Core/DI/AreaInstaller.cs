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
using LevelGeneration;
using Platform;
using UnityEngine;
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

        [Header("Configuration ScriptableObjects")]
        [SerializeField] private CombatMovementConfig _movementConfig;
        [SerializeField] private InputConfig _inputConfig;
        [SerializeField] private HexDirectionConfig _hexDirectionConfig;
        [Tooltip("Platform size/shape + hex tiling dials; auto-loads from " +
                 "Resources/LevelGeneration/PlatformShapeConfig when unset")]
        [SerializeField] private LevelGeneration.Data.PlatformShapeConfig _platformShapeConfig;

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

            InstallGameCoreBindings();
            InstallPlatformBindings();
            InstallCombatBindings();

            // NPC proximity interaction (F-prompt talk, aggro-on-approach, intent markers, name labels).
            NpcInteractionInstaller.Install(Container);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Developer state overlay (quests + director facts). Editor/dev-build only; never ships.
            DevToolsInstaller.Install(Container);
#endif
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

            // Camera Configuration from Resources
            Container.Bind<CameraConfig>().FromResource("CameraConfig").AsSingle();

            // Scene entrypoints - bind to IInitializable so Zenject calls Initialize() after injection
            Container.BindInterfacesTo<AreaSceneEntrypoint>().FromComponentInHierarchy().AsSingle();
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
        }

        private void InstallCombatStateFactories()
        {
            Container.BindFactory<CombatActiveState, CombatActiveState.Factory>();
            Container.BindFactory<CombatIdleState, CombatIdleState.Factory>();
        }
    }
}
