using UI.AbilityPreview;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Binds the shared ability-preview popover (one mechanism, two surfaces): the config, the
    /// hidden 3D stage rig, and the popover view from its prefab. The CALLING installer must
    /// bind the scene's <see cref="IAbilityPreviewHeroSource"/> first — which hero the stage
    /// shows is the per-scene seam. If the prefab is missing the popover degrades to a
    /// null object and the scene runs normally.
    /// </summary>
    public class AbilityPreviewInstaller : Installer<AbilityPreviewInstaller>
    {
        private const string ConfigResourcePath = "UI/AbilityPreviewConfig";
        private const string PopoverPrefabResourcePath = "Prefabs/UI/AbilityPreviewPopover";

        public override void InstallBindings()
        {
            var config = Resources.Load<AbilityPreviewConfig>(ConfigResourcePath);
            if (config == null)
            {
                throw new System.InvalidOperationException(
                    $"[AbilityPreviewInstaller] AbilityPreviewConfig not found at Resources/{ConfigResourcePath}.");
            }

            Container.BindInstance(config).AsSingle();

            Container.Bind<IAbilityPreviewStage>()
                .To<AbilityPreviewStageRig>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("AbilityPreviewStageRig")
                .AsSingle();

            var popoverPrefab = Resources.Load<GameObject>(PopoverPrefabResourcePath);
            if (popoverPrefab == null)
            {
                Debug.LogWarning(
                    "[AbilityPreviewInstaller] No popover prefab found at " +
                    $"Resources/{PopoverPrefabResourcePath}. Ability previews are disabled this run.");
                Container.Bind<IAbilityPreviewPopover>().To<NullAbilityPreviewPopover>().AsSingle();
                return;
            }

            Container.Bind<IAbilityPreviewPopover>()
                .To<AbilityPreviewPopoverView>()
                .FromComponentInNewPrefab(popoverPrefab)
                .AsSingle();
        }
    }
}
