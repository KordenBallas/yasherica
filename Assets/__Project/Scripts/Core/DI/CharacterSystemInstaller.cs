using System.Collections.Generic;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the modular character system. Binds the part/attachment
    /// catalogs and the character factory. Per-character object graphs (controller,
    /// swap executor, socket mounter) are deliberately factory-constructed, not
    /// container-bound: they are transient per-entity state.
    /// </summary>
    public class CharacterSystemInstaller : MonoInstaller
    {
        private const string PartDefinitionsResourcePath = "CharacterSystem/Parts";
        private const string AttachmentDefinitionsResourcePath = "CharacterSystem/Attachments";
        private const string BodyPlanConfirmPanelResourcePath = "Prefabs/UI/BodyPlanConfirmPanel";

        [Header("Data Definitions (auto-loaded from Resources when empty)")]
        [SerializeField] private List<PartDefinition> _partDefinitions;
        [SerializeField] private List<AttachmentDefinition> _attachmentDefinitions;

        public override void InstallBindings()
        {
            InstallData();
            InstallFactory();
            InstallBodyPlan();
        }

        // IGameLogger is intentionally NOT bound here: LoggingInstaller (installed by AreaInstaller)
        // is the single UnityGameLogger AsSingle binding for the scene. Zenject 6 forbids AsSingle on
        // the same concrete type across multiple bindings, so binding it again here (even with
        // IfNotBound) throws during finalization. Feature installers only resolve IGameLogger.

        private void InstallData()
        {
            var parts = LoadDefinitions(_partDefinitions, PartDefinitionsResourcePath);
            Container.Bind<IPartCatalog>()
                .To<PartCatalog>()
                .AsSingle()
                .WithArguments(parts as IReadOnlyList<PartDefinition>);

            var attachments = LoadDefinitions(_attachmentDefinitions, AttachmentDefinitionsResourcePath);
            Container.Bind<IAttachmentCatalog>()
                .To<AttachmentCatalog>()
                .AsSingle()
                .WithArguments(attachments as IReadOnlyList<AttachmentDefinition>);
        }

        private void InstallFactory()
        {
            Container.Bind<IModularCharacterFactory>().To<ModularCharacterFactory>().AsSingle();
        }

        private void InstallBodyPlan()
        {
            // The hero's visual bridge lives here (its home system); Mutation and the races
            // integration resolve it cross-installer. Lazy: only resolved by its consumers.
            Container.Bind<ModularCharacterVisual>().FromComponentInHierarchy().AsSingle();

            // Orchestrates body-plan transitions (frame changes). The confirm prompt and the
            // shed-part sink are optional injections bound by their own installers; without
            // them the coordinator degrades (auto-confirm / drop-with-warning) instead of
            // failing scene startup.
            Container.Bind<BodyPlanSwapCoordinator>().AsSingle();

            // Shed-confirm modal (FR7). Missing prefab degrades gracefully: the coordinator's
            // optional prompt stays null and shedding changes auto-confirm with a warning
            // (mirrors the MutationInstaller guard convention).
            var confirmPanelPrefab = Resources.Load<GameObject>(BodyPlanConfirmPanelResourcePath);
            if (confirmPanelPrefab == null)
            {
                Debug.LogWarning(
                    $"[CharacterSystemInstaller] Body-plan confirm panel prefab not found at Resources/{BodyPlanConfirmPanelResourcePath}; shedding body-plan changes will auto-confirm.");
                return;
            }

            Container.Bind<CharacterSystem.View.IBodyPlanConfirmView>()
                .To<CharacterSystem.View.BodyPlanConfirmView>()
                .FromComponentInNewPrefab(confirmPanelPrefab)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<CharacterSystem.View.BodyPlanConfirmPresenter>().AsSingle();
        }

        private List<TDefinition> LoadDefinitions<TDefinition>(List<TDefinition> serialized, string resourcePath)
            where TDefinition : ScriptableObject
        {
            if (serialized != null && serialized.Count > 0)
            {
                return serialized;
            }

            var loaded = new List<TDefinition>(Resources.LoadAll<TDefinition>(resourcePath));
            if (loaded.Count == 0)
            {
                Debug.LogWarning(
                    $"[CharacterSystemInstaller] No {typeof(TDefinition).Name} assets assigned or found under Resources/{resourcePath}.");
            }

            return loaded;
        }
    }
}
