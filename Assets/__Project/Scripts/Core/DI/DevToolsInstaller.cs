using DevTools;
using DevTools.Core;
using DevTools.View;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Wires the developer state overlay (a non-Mono <see cref="Installer{TDerived}"/>, so it needs no
    /// GameObject of its own). Binds the pure-C# <see cref="DevStatePresenter"/> as the
    /// <see cref="IDevStateSource"/> and spawns the thin <see cref="DevOverlayView"/> on a fresh
    /// GameObject. Installed by <c>AreaInstaller</c> only under <c>UNITY_EDITOR || DEVELOPMENT_BUILD</c>,
    /// so the overlay never ships in a release build.
    /// </summary>
    public class DevToolsInstaller : Installer<DevToolsInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<IDevStateSource>().To<DevStatePresenter>().AsSingle();

            Container.Bind<DevOverlayView>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("DevOverlay")
                .AsSingle()
                .NonLazy();
        }
    }
}
