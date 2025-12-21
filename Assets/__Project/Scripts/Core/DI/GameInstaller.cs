using Zenject;
using LevelGeneration;
using Platform;
using Battlefield;

namespace Core.DI
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Core systems
            Container.Bind<IScenarioGenerator>().To<ScenarioGenerator>().AsSingle();
            Container.Bind<IPlatformGraphGenerator>().To<PlatformGraphGenerator>().AsSingle();
            
            // Factory Registry
            Container.Bind<IPlatformFactoryRegistry>().To<PlatformFactory>().AsSingle();
            
            // Platform Factories (will be registered in PlatformInstaller)
            Container.BindFactory<SimplePlatform, SimplePlatform.Factory>();
            Container.BindFactory<CombatPlatform, CombatPlatform.Factory>();
            
            // Battlefield and Grid Factories (handled in BattlefieldInstaller)
        }
    }
}

