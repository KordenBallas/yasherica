using Zenject;
using LevelGeneration;
using Platform;
using Core.Camera;

namespace Core.DI
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Core systems
            Container.Bind<IScenarioGenerator>().To<ScenarioGenerator>().AsSingle();
            Container.Bind<IPlatformGraphGenerator>().To<PlatformGraphGenerator>().AsSingle();
            
            // Factory Registry (legacy - kept for backwards compatibility)
            Container.Bind<IPlatformFactoryRegistry>().To<PlatformFactory>().AsSingle();
            
            // Camera Service
            Container.Bind<ICameraService>().To<CameraService>().FromComponentInHierarchy().AsSingle();
            
            // Camera Configuration from Resources
            Container.Bind<CameraConfig>().FromResource("CameraConfig").AsSingle();
            
            // Scene entrypoints - bind to IInitializable so Zenject calls Initialize() after injection
            Container.BindInterfacesTo<AreaSceneEntrypoint>().FromComponentInHierarchy().AsSingle();
            
            // Note: Battlefield bindings are now in CombatInstaller (Combat module internal)
        }
    }
}

