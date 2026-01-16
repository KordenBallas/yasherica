using Platform;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for platform system dependencies.
    /// Binds platform factory, state factory, and all state factories.
    /// </summary>
    public class PlatformInstaller : MonoInstaller
    {
        public override void InstallBindings()
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
    }
}
