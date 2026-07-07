using GameInput.Core;
using GameInput.View;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Wires the cross-device input foundation: the binding catalog (the declarative action × source
    /// table), the shared runtime actions loaded from <c>Resources/Input/GameActions</c>, the
    /// active-source tracker (last device used wins), the prompt cue provider every prompt call site
    /// reads, and the touch control overlay (self-hides without a touchscreen). Installed by each
    /// scene installer (Area, Hub, Arena). All device bindings live on the actions simultaneously, so
    /// there is no per-platform controller switching.
    /// </summary>
    public class InputInstaller : Installer<InputInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<InputBindingCatalog>().AsSingle();
            Container.Bind<InputSourceClassifier>().AsSingle();
            Container.BindInterfacesAndSelfTo<ActiveInputSourceTracker>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameActionsProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<PromptCueProvider>().AsSingle();

            Container.Bind<TouchControlsView>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("TouchControls")
                .AsSingle()
                .NonLazy();
        }
    }
}
