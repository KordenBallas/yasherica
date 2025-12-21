using Zenject;
using Platform;
using LevelGeneration;

namespace Core.DI
{
    public class PlatformInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Platform factories
            Container.BindFactory<SimplePlatform, SimplePlatform.Factory>();
            Container.BindFactory<CombatPlatform, CombatPlatform.Factory>();
            
            // Register factories in registry
            var registry = Container.Resolve<IPlatformFactoryRegistry>();
            registry.RegisterFactory(PlatformType.Simple, Container.Resolve<SimplePlatform.Factory>());
            registry.RegisterFactory(PlatformType.Combat, Container.Resolve<CombatPlatform.Factory>());
        }
    }
}

