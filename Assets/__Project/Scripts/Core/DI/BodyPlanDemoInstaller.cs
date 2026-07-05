using Character;
using CharacterSystem.View;
using Inventory.Core;
using Inventory.Presenter;
using Inventory.View;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Dev-only installer for the body-plan demo scene (built by
    /// Tools/Character System/Build Body-Plan Demo Scene). Provides the minimal environment
    /// the hero + body-plan pipeline need outside the Area scene: the logger, the character
    /// registry (the movement controller self-registers), the part stash (so shed parts are
    /// visible in the demo), and the part-selection console. The character system itself
    /// (catalogs, factory, coordinator, confirm modal) and locomotion come from their own
    /// installers on the same SceneContext.
    /// </summary>
    public class BodyPlanDemoInstaller : MonoInstaller
    {
        private const string PartInventoryPanelResourcePath = "Prefabs/UI/PartInventoryPanel";

        public override void InstallBindings()
        {
            // The single IGameLogger home for this scene (AreaInstaller does this in Area).
            LoggingInstaller.Install(Container);

            // The hero's CharacterMovementController [Inject]s the registry and self-registers.
            Container.Bind<ICharacterRegistry>()
                .To<CharacterRegistry>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();

            InstallPartStash();

            Container.Bind<IBodyPlanDemoConsoleView>()
                .To<BodyPlanDemoConsoleView>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<BodyPlanDemoConsolePresenter>().AsSingle().NonLazy();
        }

        /// <summary>Same part-stash trio InventoryInstaller binds in the Area scene, so a
        /// frame change's shed parts land somewhere visible here too.</summary>
        private void InstallPartStash()
        {
            Container.Bind<IPartInventoryModel>().To<PartInventoryModel>().AsSingle();
            Container.Bind<CharacterSystem.Core.IShedPartSink>()
                .To<Inventory.Integration.PartInventorySink>()
                .AsSingle();

            var panelPrefab = Resources.Load<PartInventoryView>(PartInventoryPanelResourcePath);
            if (panelPrefab == null)
            {
                Debug.LogWarning(
                    $"[BodyPlanDemoInstaller] Part inventory panel prefab not found at Resources/{PartInventoryPanelResourcePath}; shed parts are stored but not shown.");
                return;
            }

            Container.Bind<IPartInventoryView>()
                .To<PartInventoryView>()
                .FromComponentInNewPrefab(panelPrefab)
                .AsSingle()
                .NonLazy();

            Container.BindInterfacesAndSelfTo<PartInventoryPresenter>().AsSingle().NonLazy();
        }
    }
}
