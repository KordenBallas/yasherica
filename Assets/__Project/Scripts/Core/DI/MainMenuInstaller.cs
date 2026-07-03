using Core.SceneFlow;
using MainMenu;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the MainMenu scene: the logging home (per-container, see
    /// <see cref="LoggingInstaller"/>), the scene loader, and the menu MVP pair. The menu is
    /// deliberately minimal — it must never pull gameplay bindings; each mode's scene carries
    /// its own SceneContext.
    /// </summary>
    public class MainMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            LoggingInstaller.Install(Container);

            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
            Container.Bind<IMainMenuView>().To<MainMenuView>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesTo<MainMenuPresenter>().AsSingle().NonLazy();
        }
    }
}
