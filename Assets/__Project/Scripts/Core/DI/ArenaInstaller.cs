using Combat.Arena;
using Combat.Arena.Core;
using Combat.Arena.Data;
using Combat.Arena.Networking;
using Combat.Arena.View;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Combat.Execution;
using Combat.TurnManagement;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the Arena scene: the combat subset of <see cref="AreaInstaller"/>
    /// (execution, validation, battlefield, telegraph, planning input — everything the round
    /// needs) plus the Arena round loop (commit/collect/order/resolve over the lockstep
    /// transport). Deliberately absent: narrative, mutation, loot, inventory, streaming world —
    /// the arena is Pillar-4 combat only.
    /// </summary>
    public class ArenaInstaller : MonoInstaller
    {
        /// <summary>The default ability queue depth (matches <see cref="AreaInstaller"/>).</summary>
        private const int MaxAbilityQueueSizeDefault = 3;

        private const string MovementConfigResourcePath = "Configs/CombatMovementConfig";
        private const string InputConfigResourcePath = "Configs/InputConfig";
        private const string HexDirectionConfigResourcePath = "Configs/HexDirectionConfig";
        private const string PlatformShapeConfigResourcePath = "LevelGeneration/PlatformShapeConfig";
        private const string HeroDefinitionResourcePath = "Heroes/TestHeroDefinition";
        private const string MatchConfigResourcePath = "Arena/ArenaMatchConfig";
        private const string DraftConfigResourcePath = "Arena/ArenaDraftConfig";
        private const string DraftPanelResourcePath = "Prefabs/UI/ArenaDraftPanel";
        private const string PartInfoPopoverResourcePath = "Prefabs/UI/ArenaPartInfoPopover";
        private const string DifficultyResourcePath = "Combat/Difficulty/NormalDifficulty";

        [Header("Configuration ScriptableObjects (auto-loaded from Resources when empty)")]
        [SerializeField] private CombatMovementConfig _movementConfig;
        [SerializeField] private InputConfig _inputConfig;
        [SerializeField] private HexDirectionConfig _hexDirectionConfig;
        [SerializeField] private LevelGeneration.Data.PlatformShapeConfig _platformShapeConfig;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private ArenaMatchConfig _matchConfig;
        [SerializeField] private ArenaDraftConfig _draftConfig;
        [Tooltip("Global AI-dummy difficulty preset; auto-loads " +
                 "Resources/Combat/Difficulty/NormalDifficulty when unset. Missing = neutral (sharp AI)")]
        [SerializeField] private DifficultyDefinition _difficulty;

        public override void InstallBindings()
        {
            LoggingInstaller.Install(Container);

            // Save-file stores only (no run restore/autosave — those are Area services): the
            // draft reads the tasted-forms catalog straight from meta.json (P4-5 req 3).
            PersistenceInstaller.Install(Container);
            Container.Bind<ArenaTastedCatalogReader>().AsSingle();

            // Cross-device input foundation: shared actions, active-source tracking, prompt cues,
            // and the touch overlay.
            InputInstaller.Install(Container);

            InstallConfigurations();
            InstallCombatSubset();
            InstallArenaBindings();
            InstallNetworkingBindings();

            Container.BindInterfacesTo<ArenaSceneEntrypoint>().FromComponentInHierarchy().AsSingle();
        }

        private void InstallConfigurations()
        {
            _movementConfig = LoadIfNull(_movementConfig, MovementConfigResourcePath);
            _inputConfig = LoadIfNull(_inputConfig, InputConfigResourcePath);
            _hexDirectionConfig = LoadIfNull(_hexDirectionConfig, HexDirectionConfigResourcePath);
            _heroDefinition = LoadIfNull(_heroDefinition, HeroDefinitionResourcePath);
            _matchConfig = LoadIfNull(_matchConfig, MatchConfigResourcePath);
            _draftConfig = LoadIfNull(_draftConfig, DraftConfigResourcePath);

            Container.BindInstance(_movementConfig).AsSingle();
            Container.BindInstance(_inputConfig).AsSingle();
            Container.BindInstance(_hexDirectionConfig).AsSingle();
            Container.BindInstance(_heroDefinition).AsSingle();
            Container.BindInstance(_matchConfig).AsSingle();
            Container.BindInstance(_draftConfig).AsSingle();

            // The SO → Core bridge for the draft (settings are what the host/model consume).
            Container.Bind<ArenaDraftSettings>()
                .FromMethod(ctx => ArenaDraftConfigMapper.ToSettings(
                    _draftConfig, _matchConfig.MaxPlayers, ctx.Container.Resolve<Core.Logging.IGameLogger>()))
                .AsSingle();

            if (_platformShapeConfig == null)
            {
                _platformShapeConfig = Resources.Load<LevelGeneration.Data.PlatformShapeConfig>(
                    PlatformShapeConfigResourcePath);
            }

            var shapeSettings = LevelGeneration.Data.PlatformShapeConfigMapper.ToSettings(_platformShapeConfig);
            Container.Bind<LevelGeneration.Surface.PlatformShapeSettings>()
                .FromInstance(shapeSettings)
                .AsSingle();

            Container.Bind<CombatConfig>().AsSingle().WithArguments(
                shapeSettings.HexSize,
                shapeSettings.Orientation,
                MaxAbilityQueueSizeDefault);
        }

        private void InstallCombatSubset()
        {
            // Registries + planning input (the same stack the PvE Act phase uses).
            Container.Bind<Character.ICharacterRegistry>()
                .To<Character.CharacterRegistry>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();

            Container.Bind<IPlayerRegistry>().To<PlayerRegistry>().AsSingle();

            Container.Bind<Combat.Input.IAimDirectionResolver>()
                .To<Combat.Input.AimDirectionResolver>()
                .AsSingle();
            Container.Bind<Combat.Input.IInputController>()
                .To<Combat.Input.CombatInputController>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();

            Container.Bind<Combat.View.ICombatActionPanelView>()
                .FromComponentInHierarchy()
                .AsCached()
                .IfNotBound();

            Container.Bind<Combat.Animation.ICharacterMovementAnimator>()
                .To<Combat.Animation.SimpleLerpAnimator>()
                .AsSingle();

            // Abilities + effects.
            Container.Bind<IStatusEffectFactory>().To<StatusEffectFactory>().AsSingle();
            Container.Bind<IAbilityFactory>().To<AbilityFactory>().AsSingle();
            Container.Bind<StatusEffectTriggerProcessor>().AsSingle();

            // Execution core (all pure C#).
            Container.Bind<IDamageSystem>().To<DamageSystem>().AsSingle();
            Container.Bind<IAbilityShapeCalculator>().To<AbilityShapeCalculator>().AsSingle();
            Container.Bind<IAbilityExecutor>().To<AbilityExecutor>().AsSingle();
            Container.Bind<IActionExecutor>().To<ActionExecutor>().AsSingle();
            Container.Bind<IActionValidator>().To<ActionValidator>().AsSingle();
            Container.Bind<RoundLifecycleProcessor>().AsSingle();
            Container.Bind<EnemyIntentResolver>().AsSingle();

            // Telegraph presentation support.
            Container.Bind<IAbilityOutcomeCalculator>().To<AbilityOutcomeCalculator>().AsSingle();

            // Enemy AI (P2-4): the arena is free-for-all — every other unit is a target; the
            // global difficulty preset tunes the offline dummies' decision quality. Missing
            // asset degrades softly to neutral (sharp) instead of failing the install.
            Container.Bind<Combat.Player.AI.IHostilityPolicy>()
                .To<Combat.Player.AI.FreeForAllHostilityPolicy>().AsSingle();
            if (_difficulty == null)
            {
                _difficulty = Resources.Load<DifficultyDefinition>(DifficultyResourcePath);
                if (_difficulty == null)
                {
                    Debug.LogWarning("[ArenaInstaller] DifficultyDefinition not assigned and not found at " +
                                     $"Resources/{DifficultyResourcePath} — AI dummies run at neutral (sharp) difficulty.");
                }
            }
            var difficulty = _difficulty;
            Container.Bind<Combat.Player.AI.IAIDifficultySource>()
                .FromMethod(_ => new Combat.Player.AI.StaticAIDifficultySource(
                    DifficultyDefinitionMapper.ToSettings(difficulty)))
                .AsSingle();
            Container.Bind<Combat.Player.AI.AIDecisionMakerFactory>().AsSingle();
            Container.Bind<Combat.Data.Providers.IAbilityDefinitionCatalog>()
                .To<Combat.Data.Providers.AbilityDefinitionCatalog>().AsSingle();
            Container.Bind<Combat.Data.Providers.IStatusEffectDefinitionCatalog>()
                .To<Combat.Data.Providers.StatusEffectDefinitionCatalog>().AsSingle();
            Container.Bind<Combat.View.ICombatUnitViewRegistry>()
                .To<Combat.View.CombatUnitViewRegistry>().AsSingle();

            // Battlefield + turn bookkeeping + resolve pacing.
            Container.BindFactory<IBattlefield, BattlefieldFactory>().To<Battlefield>();
            Container.Bind<ITurnManager>().To<TurnManager>().AsSingle();
            Container.Bind<Combat.Player.EnemyRoundController>().AsSingle();
        }

        private void InstallArenaBindings()
        {
            Container.Bind<ArenaMatchContext>().AsSingle();
            Container.Bind<ArenaSeatLedger>().AsSingle();
            Container.Bind<IArenaStatusReconstructor>().To<CatalogStatusReconstructor>().AsSingle();
            Container.Bind<ArenaSnapshotRestorer>().AsSingle();
            Container.Bind<ArenaSeatStatusMirror>().AsSingle();

            // X2 anti-cheat: validator + volley-credit book; the entrypoint arms the match host
            // when the config gate is on (the canonical state source is the controller itself).
            Container.Bind<ArenaCommitValidator>().AsSingle();
            Container.Bind<ArenaQueueCreditLedger>().AsSingle();
            Container.Bind<IArenaCanonicalStateSource>().To<ArenaCombatController>().FromResolve();

            // P4-3b: the batch eligibility doubles as the resolver's optional policy — inert
            // outside batch mode (it then mirrors the default skip-dead rule), armed by the
            // entrypoint when the config gate is on.
            Container.BindInterfacesAndSelfTo<ArenaBatchEligibility>().AsSingle();
            Container.Bind<ArenaCommitBuilder>().AsSingle();
            Container.Bind<ArenaCommitCollector>().AsSingle();

            // The resolution-order strategy is a config pick (P4-3a); rotation stays the default.
            if (_matchConfig.ResolutionOrderMode == ArenaResolutionOrderMode.SeededShuffle)
            {
                Container.Bind<IArenaResolutionOrder>().To<SeededShuffleResolutionOrder>().AsSingle();
            }
            else
            {
                Container.Bind<IArenaResolutionOrder>().To<RotatingInitiativeOrder>().AsSingle();
            }
            Container.Bind<LastHeroStandingWinCondition>().AsSingle();
            Container.Bind<ArenaSpawnPlanner>().AsSingle();
            Container.Bind<ArenaPlayerDirectory>().AsSingle();

            // The lockstep transport: NGO named messages for the networked session, the
            // in-process loopback when the config runs the offline dev fallback.
            if (_matchConfig.OfflineMode)
            {
                Container.Bind<IArenaTransport>().To<LoopbackArenaTransport>().AsSingle();
            }
            else
            {
                Container.BindInterfacesAndSelfTo<NgoArenaTransport>().AsSingle();
            }

            Container.Bind<ArenaMatchHost>().AsSingle();
            Container.Bind<ArenaAICommitSource>().AsSingle();

            // The parts draft (P4-5): catalog exchange → host-composed board → snake picks →
            // per-seat loadouts, all above the untouched round loop.
            Container.Bind<IArenaDraftClock>().To<UnityArenaDraftClock>().AsSingle();
            Container.Bind<IArenaDraftPartInfoSource>().To<PartCatalogDraftInfoSource>().AsSingle();
            Container.BindInterfacesAndSelfTo<ArenaTastedCatalogRegistry>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ArenaTastedCatalogSender>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ArenaDraftHost>().AsSingle();
            Container.BindInterfacesAndSelfTo<ArenaDraftFlow>().AsSingle().NonLazy();

            InstallDraftScreen();

            Container.Bind<ArenaCombatController>().AsSingle();
            Container.Bind<ICombatController>().To<ArenaCombatController>().FromResolve();

            Container.Bind<ArenaPlatformBuilder>().AsSingle();
            Container.Bind<ArenaHeroSpawner>().AsSingle();

            // Drafted parts → combat abilities: the same part→combat mapping PvE uses
            // (IPartCatalog comes from CharacterSystemInstaller on this SceneContext).
            Container.Bind<Combat.Integration.IPartAbilityResolver>()
                .To<Combat.Integration.PartAbilityResolver>()
                .AsSingle();
        }

        private void InstallDraftScreen()
        {
            // The 3D board/hero stage + the shared ability-preview popover. The hero source is
            // the draft's own (base assembly + drafted parts) — NEVER the scene's
            // ModularCharacterVisual, which is ambiguous once several heroes spawn.
            Container.Bind<IArenaDraftStage>()
                .To<ArenaDraftStageRig>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("ArenaDraftStageRig")
                .AsSingle();
            Container.Bind<UI.AbilityPreview.IAbilityPreviewHeroSource>()
                .To<ArenaDraftHeroSource>()
                .AsSingle();
            AbilityPreviewInstaller.Install(Container);

            // Panel + part-info popover from prefabs (mirrors MutationInstaller's guard: a
            // missing prefab disables the screen, never crashes the scene — the presenter
            // then runs the draft headless off the host's auto-picks).
            var panelPrefab = Resources.Load<GameObject>(DraftPanelResourcePath);
            if (panelPrefab != null)
            {
                Container.Bind<IArenaDraftView>()
                    .To<ArenaDraftView>()
                    .FromComponentInNewPrefab(panelPrefab)
                    .AsSingle();
            }
            else
            {
                Debug.LogWarning(
                    "[ArenaInstaller] No draft panel prefab at " +
                    $"Resources/{DraftPanelResourcePath}. The draft runs headless this run.");
            }

            var popoverPrefab = Resources.Load<GameObject>(PartInfoPopoverResourcePath);
            if (popoverPrefab != null)
            {
                Container.Bind<IArenaPartInfoPopover>()
                    .To<ArenaPartInfoPopoverView>()
                    .FromComponentInNewPrefab(popoverPrefab)
                    .AsSingle();
            }
            else
            {
                Debug.LogWarning(
                    "[ArenaInstaller] No part-info popover prefab at " +
                    $"Resources/{PartInfoPopoverResourcePath}. Part info is disabled this run.");
            }

            Container.BindInterfacesAndSelfTo<ArenaDraftPresenter>().AsSingle().NonLazy();
        }

        private void InstallNetworkingBindings()
        {
            Container.Bind<Unity.Netcode.NetworkManager>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<ArenaSessionService>().AsSingle();
            Container.Bind<ArenaMatchLauncher>().AsSingle();

            // The panel starts active in the scene (the presenter hides it in offline mode).
            Container.Bind<IArenaConnectView>()
                .To<ArenaConnectView>()
                .FromComponentInHierarchy()
                .AsSingle();
            Container.BindInterfacesTo<ArenaConnectPresenter>().AsSingle().NonLazy();

            // In-match HUD: status/spectate/winner/leave (Leave loads the main menu).
            Container.Bind<Core.SceneFlow.ISceneLoader>().To<Core.SceneFlow.SceneLoader>().AsSingle();
            Container.Bind<IArenaMatchHudView>()
                .To<ArenaMatchHudView>()
                .FromComponentInHierarchy()
                .AsSingle();
            Container.BindInterfacesTo<ArenaMatchHudPresenter>().AsSingle().NonLazy();

            // X1 reconnect / desync-recovery / host-migration coordinators. Constructed in every
            // mode (cheap, inert until armed); the entrypoint arms them on the networked path only.
            Container.Bind<IArenaReconnectClock>().To<UnityArenaReconnectClock>().AsSingle();
            Container.Bind<IArenaLocalEndpointSource>().To<LanEndpointSource>().AsSingle();
            Container.Bind<ArenaReconnectHost>().AsSingle();
            Container.Bind<ArenaReconnectClient>().AsSingle();
            Container.BindInterfacesTo<ArenaReconnectTicker>().AsSingle();
        }

        private TConfig LoadIfNull<TConfig>(TConfig current, string resourcePath)
            where TConfig : ScriptableObject
        {
            if (current != null)
            {
                return current;
            }

            var loaded = Resources.Load<TConfig>(resourcePath);
            if (loaded == null)
            {
                throw new System.InvalidOperationException(
                    $"[ArenaInstaller] {typeof(TConfig).Name} not assigned and not found at Resources/{resourcePath}.");
            }

            return loaded;
        }
    }
}
