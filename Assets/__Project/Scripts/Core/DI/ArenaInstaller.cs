using Combat.Arena;
using Combat.Arena.Core;
using Combat.Arena.Data;
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

        [Header("Configuration ScriptableObjects (auto-loaded from Resources when empty)")]
        [SerializeField] private CombatMovementConfig _movementConfig;
        [SerializeField] private InputConfig _inputConfig;
        [SerializeField] private HexDirectionConfig _hexDirectionConfig;
        [SerializeField] private LevelGeneration.Data.PlatformShapeConfig _platformShapeConfig;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private ArenaMatchConfig _matchConfig;

        public override void InstallBindings()
        {
            LoggingInstaller.Install(Container);

            InstallConfigurations();
            InstallCombatSubset();
            InstallArenaBindings();

            Container.BindInterfacesTo<ArenaSceneEntrypoint>().FromComponentInHierarchy().AsSingle();
        }

        private void InstallConfigurations()
        {
            _movementConfig = LoadIfNull(_movementConfig, MovementConfigResourcePath);
            _inputConfig = LoadIfNull(_inputConfig, InputConfigResourcePath);
            _hexDirectionConfig = LoadIfNull(_hexDirectionConfig, HexDirectionConfigResourcePath);
            _heroDefinition = LoadIfNull(_heroDefinition, HeroDefinitionResourcePath);
            _matchConfig = LoadIfNull(_matchConfig, MatchConfigResourcePath);

            Container.BindInstance(_movementConfig).AsSingle();
            Container.BindInstance(_inputConfig).AsSingle();
            Container.BindInstance(_hexDirectionConfig).AsSingle();
            Container.BindInstance(_heroDefinition).AsSingle();
            Container.BindInstance(_matchConfig).AsSingle();

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

            Container.Bind<Combat.Input.IInputController>()
                .To<Combat.Input.PCInputController>()
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
            Container.Bind<Combat.Data.Providers.IAbilityDefinitionCatalog>()
                .To<Combat.Data.Providers.AbilityDefinitionCatalog>().AsSingle();
            Container.Bind<Combat.View.ICombatUnitViewRegistry>()
                .To<Combat.View.CombatUnitViewRegistry>().AsSingle();

            // Battlefield + turn bookkeeping + resolve pacing.
            Container.BindFactory<IBattlefield, BattlefieldFactory>().To<Battlefield>();
            Container.Bind<ITurnManager>().To<TurnManager>().AsSingle();
            Container.Bind<Combat.Player.EnemyRoundController>().AsSingle();
        }

        private void InstallArenaBindings()
        {
            Container.Bind<ArenaCommitBuilder>().AsSingle();
            Container.Bind<ArenaCommitCollector>().AsSingle();
            Container.Bind<IArenaResolutionOrder>().To<RotatingInitiativeOrder>().AsSingle();
            Container.Bind<LastHeroStandingWinCondition>().AsSingle();
            Container.Bind<ArenaSpawnPlanner>().AsSingle();

            // Phase 2: in-process lockstep (offline vs dummies). Phase 3 swaps in the NGO transport.
            Container.Bind<IArenaTransport>().To<LoopbackArenaTransport>().AsSingle();
            Container.Bind<ArenaMatchHost>().AsSingle();
            Container.Bind<ArenaAICommitSource>().AsSingle();

            Container.Bind<ArenaCombatController>().AsSingle();
            Container.Bind<ICombatController>().To<ArenaCombatController>().FromResolve();

            Container.Bind<ArenaPlatformBuilder>().AsSingle();
            Container.Bind<ArenaHeroSpawner>().AsSingle();
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
