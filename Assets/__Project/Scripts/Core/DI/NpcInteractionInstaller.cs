using Narrative.Interaction;
using Narrative.Interaction.Core;
using Narrative.Interaction.Data;
using Narrative.Interaction.View;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Wires the NPC proximity interaction system: the global radius settings (from the
    /// <c>NpcInteractionConfig</c> SO, with code defaults if unwired), the run-scoped interaction registry
    /// + binder service, the pure intent/proximity logic, the encounter starter, the F-button input
    /// adapter, and the per-frame <see cref="NpcProximityPresenter"/>. The dev radius overlay is bound only
    /// in editor/development builds. Installed by <c>AreaInstaller</c>.
    /// </summary>
    public class NpcInteractionInstaller : Installer<NpcInteractionInstaller>
    {
        private const string ConfigResourcePath = "Narrative/NpcInteractionConfig";

        public override void InstallBindings()
        {
            var config = Resources.Load<NpcInteractionConfig>(ConfigResourcePath);
            Container.Bind<NpcInteractionSettings>()
                .FromInstance(NpcInteractionConfigMapper.ToSettings(config))
                .AsSingle();

            Container.Bind<NpcIntentResolver>().AsSingle();
            Container.Bind<ProximityEvaluator>().AsSingle();
            Container.Bind<NpcEncounterStarter>().AsSingle();

            Container.Bind<INpcInteractionRegistry>().To<NpcInteractionRegistry>().AsSingle();
            Container.Bind<INpcInteractionService>().To<NpcInteractionService>().AsSingle();

            Container.Bind<IInteractionInput>()
                .To<NpcInteractionInput>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("NpcInteractionInput")
                .AsSingle();

            Container.BindInterfacesAndSelfTo<NpcProximityPresenter>().AsSingle().NonLazy();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev radius overlay (F2). Editor/dev-build only; never ships as player UX (R14).
            Container.Bind<NpcRadiusDebugView>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("NpcRadiusDebug")
                .AsSingle()
                .NonLazy();
#endif
        }
    }
}
