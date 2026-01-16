using System.Collections.Generic;
using Character;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Combat.Data.Providers;
using Combat.Execution;
using Combat.Integration;
using Combat.TurnManagement;
using Combat.View;
using Platform;
using UnityEngine;
using Zenject;

namespace Combat.DI
{
    /// <summary>
    /// Zenject installer for combat system dependencies.
    /// Binds all combat services for dependency injection.
    /// </summary>
    public class CombatInstaller : MonoInstaller
    {
        [Header("Configuration ScriptableObjects")]
        [SerializeField] private CombatMovementConfig _movementConfig;
        [SerializeField] private InputConfig _inputConfig;
        [SerializeField] private HexDirectionConfig _hexDirectionConfig;

        [Header("Data Definitions (Optional - for data-driven system)")]
        [Tooltip("Status effect definitions for the data-driven system")]
        [SerializeField] private List<StatusEffectDefinition> _statusEffectDefinitions;

        [Tooltip("Ability definitions for the data-driven system")]
        [SerializeField] private List<AbilityDefinition> _abilityDefinitions;

        [Tooltip("Enemy definitions for the data-driven system")]
        [SerializeField] private List<EnemyDefinition> _enemyDefinitions;
        
        public override void InstallBindings()
        {
            // Configuration ScriptableObjects - Fail-fast if not assigned
            if (_movementConfig == null)
            {
                throw new System.InvalidOperationException(
                    "[CombatInstaller] CombatMovementConfig not assigned! " +
                    "Assign the config asset in the scene's CombatInstaller component. " +
                    "The asset should be located in Resources/CombatMovementConfig.asset");
            }
            Container.BindInstance(_movementConfig).AsSingle();
            
            if (_inputConfig == null)
            {
                throw new System.InvalidOperationException(
                    "[CombatInstaller] InputConfig not assigned! " +
                    "Assign the config asset in the scene's CombatInstaller component. " +
                    "The asset should be located in Resources/InputConfig.asset");
            }
            Container.BindInstance(_inputConfig).AsSingle();
            
            if (_hexDirectionConfig == null)
            {
                throw new System.InvalidOperationException(
                    "[CombatInstaller] HexDirectionConfig not assigned! " +
                    "Assign the config asset in the scene's CombatInstaller component. " +
                    "The asset should be located in Resources/HexDirectionConfig.asset");
            }
            Container.BindInstance(_hexDirectionConfig).AsSingle();
            
            // Combat configuration (legacy)
            Container.Bind<CombatConfig>().AsSingle().WithArguments(
                2f,  // hexCellSize - default value
                HexOrientation.Flat  // hexOrientation - default value
            );
            
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
            Container.Bind<Input.IInputController>()
                .To<Input.PCInputController>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();
            
            // Animation Strategies
            Container.Bind<ICharacterMovementAnimator>()
                .To<SimpleLerpAnimator>()
                .AsSingle();
            
            Container.Bind<CombatEntryAnimator>().AsSingle();

            // Services
            Container.Bind<CharacterCombatInitializer>().AsSingle();

            // Status Effect Factory (bind first as abilities may need it)
            Container.Bind<IStatusEffectFactory>()
                .To<StatusEffectFactory>()
                .AsSingle();

            // Ability Factory
            Container.Bind<IAbilityFactory>()
                .To<AbilityFactory>()
                .AsSingle();

            // Status Effect Trigger Processor
            Container.Bind<StatusEffectTriggerProcessor>()
                .AsSingle();

            // Ability Data Provider (optional - for data-driven system)
            if (_abilityDefinitions != null && _abilityDefinitions.Count > 0)
            {
                Container.Bind<IAbilityDataProvider>()
                    .To<ScriptableObjectAbilityProvider>()
                    .AsSingle()
                    .WithArguments(_abilityDefinitions as IReadOnlyList<AbilityDefinition>);
            }

            // Enemy combat integration
            // Use ScriptableObject provider if definitions are available, otherwise fallback to simple provider
            if (_enemyDefinitions != null && _enemyDefinitions.Count > 0)
            {
                Container.Bind<Data.IEnemyDataProvider>()
                    .To<ScriptableObjectEnemyDataProvider>()
                    .AsSingle()
                    .WithArguments(_enemyDefinitions as IReadOnlyList<EnemyDefinition>);
            }
            else
            {
                Container.Bind<Data.IEnemyDataProvider>()
                    .To<Data.SimpleEnemyDataProvider>()
                    .AsSingle();
            }

            Container.Bind<EnemyCombatIntegrator>()
                .AsSingle();

            // AI turn controller
            Container.Bind<Player.AITurnController>()
                .AsSingle();

            // Battlefield bindings (internal to Combat)
            Container.BindFactory<FlatHexGrid, FlatHexGrid.Factory>();
            Container.BindFactory<PointyHexGrid, PointyHexGrid.Factory>();
            Container.BindFactory<IBattlefield, BattlefieldFactory>().To<Battlefield.Battlefield>();
            Container.Bind<IHexGridFactory>().To<HexGridFactory>().AsSingle();
            
            // Combat platform state
            /*Container.Bind<IPlatformState>()
                .To<CombatPlatformActiveState>()
                .AsTransient()
                .WhenInjectedInto<CombatPlatform>();*/
            
            // Core systems
            Container.Bind<IDamageSystem>().To<DamageSystem>().AsSingle();
            Container.Bind<IAbilityExecutor>().To<AbilityExecutor>().AsSingle();
            Container.Bind<IActionExecutor>().To<ActionExecutor>().AsSingle();
            Container.Bind<IActionValidator>().To<ActionValidator>().AsSingle();
            
            // Turn management
            Container.Bind<ITurnManager>().To<TurnManager>().AsSingle();
            
            // Combat controller
            Container.Bind<ICombatController>()
                .To<CombatController>()
                .AsSingle();

            // Combat controller factory
            Container.Bind<IFactory<ICombatController>>()
                .To<CombatControllerFactory>()
                .AsSingle();

            // Battlefield integration
            Container.Bind<CombatBattlefield>().AsSingle();
            
            // Rules
            Container.Bind<Rules.MovementRules>().AsSingle();
            Container.Bind<Rules.AbilityRules>().AsSingle();
        }
    }
}

