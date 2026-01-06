using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Execution;
using Combat.Integration;
using Combat.TurnManagement;
using Combat.View;
using Platform;
using Zenject;

namespace Combat.DI
{
    /// <summary>
    /// Zenject installer for combat system dependencies.
    /// Binds all combat services for dependency injection.
    /// </summary>
    public class CombatInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Combat configuration
            Container.Bind<CombatConfig>().AsSingle().WithArguments(
                2f,  // hexCellSize - default value
                HexOrientation.Flat  // hexOrientation - default value
            );
            
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

