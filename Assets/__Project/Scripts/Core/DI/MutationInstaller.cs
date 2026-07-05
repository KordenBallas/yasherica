using System.Collections.Generic;
using CharacterSystem.Runtime;
using Mutation.Application;
using Mutation.Core;
using Mutation.Data;
using Mutation.Data.Definitions;
using Mutation.Infrastructure;
using Mutation.Presenter;
using Mutation.View;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the mutation subsystem (Socketed Blanks). Binds the archetype catalog
    /// (species/passport markers + card tints), the blank catalog/rack/socketing domain, the unseal
    /// variant choice (part catalog + variant builder + presenter + card view, with an adapter onto
    /// the live modular character), the cauldron-voice trend seam, and a startup validator that
    /// warns about bad authoring. The variant builder reuses the InventoryInstaller's fusion
    /// grammar so socket interaction and cauldron fusion speak the same language.
    /// </summary>
    public class MutationInstaller : MonoInstaller
    {
        private const string ArchetypeDefinitionsResourcePath = "Mutation/Archetypes";
        private const string BlankDefinitionsResourcePath = "Mutation/Blanks";
        private const string MutationConfigResourcePath = "Mutation/MutationConfig";
        private const string ChoicePanelResourcePath = "Prefabs/UI/MutationChoicePanel";

        [Header("Configuration (auto-loaded from Resources when empty)")]
        [SerializeField] private MutationConfig _config;

        [Header("Data Definitions (auto-loaded from Resources when empty)")]
        [SerializeField] private List<ArchetypeDefinition> _archetypeDefinitions;
        [SerializeField] private List<PartBlankDefinition> _blankDefinitions;

        [Header("Stage-up choice UI (auto-loaded from Resources when empty)")]
        [Tooltip("MutationChoicePanel prefab (the authored card-hand panel under Resources/Prefabs/UI)")]
        [SerializeField] private GameObject _choicePanelPrefab;

        public override void InstallBindings()
        {
            // IGameLogger is provided by LoggingInstaller (installed by AreaInstaller); we only resolve
            // it. Re-binding UnityGameLogger here would trip Zenject 6's "AsSingle multiple times for
            // the same concrete type" assert, even with IfNotBound.
            var archetypes = LoadArchetypeDefinitions();
            Container.Bind<IArchetypeCatalog>()
                .To<ArchetypeCatalog>()
                .AsSingle()
                .WithArguments(archetypes as IReadOnlyList<ArchetypeDefinition>);

            var config = LoadConfig();
            Container.BindInstance(config).AsSingle();

            InstallVariantChoice();
            InstallSocketedBlanks(config);

            // NonLazy so the authoring validation always runs at startup. Validates the parts'
            // mutation affinities against the archetype catalog (CharacterSystem part catalog),
            // the parts' trait affinities against the trait catalog, and the blanks.
            Container.BindInterfacesAndSelfTo<MutationContentValidator>()
                .AsSingle()
                .NonLazy();
        }

        private void InstallSocketedBlanks(MutationConfig config)
        {
            // The blank catalog serves both the Core port (socket counts, slots) and the
            // data-layer icon lookup from one instance.
            var blanks = LoadBlankDefinitions();
            Container.Bind(typeof(IPartBlankCatalog), typeof(IPartBlankDataSource))
                .To<PartBlankCatalog>()
                .AsSingle()
                .WithArguments(blanks as IReadOnlyList<PartBlankDefinition>);

            // The rack cap IS the multi-track incubation tension - authored, not magic.
            Container.Bind<IBlankRack>()
                .To<BlankRack>()
                .AsSingle()
                .WithArguments(config.BlankRackCapacity);

            Container.Bind<ISocketingModel>().To<SocketingModel>().AsSingle();

            // The variant builder reuses the cauldron's fusion grammar (InventoryInstaller
            // bindings: EmergentFusionCalculator, TraitFusionRuleSet, FusionSettings) so
            // socket interaction and cauldron fusion speak the same language.
            Container.Bind<IBlankVariantBuilder>().To<BlankVariantBuilder>().AsSingle();

            // The rack view lives on the InventoryStage scene instance, left of the
            // cauldron; the presenter seeds the starting blanks and drives it.
            Container.Bind<IBlankRackView>()
                .To<BlankRackView>()
                .FromComponentInHierarchy()
                .AsSingle();
            Container.BindInterfacesAndSelfTo<BlankRackPresenter>().AsSingle().NonLazy();

            // The cauldron-voice seam: computes the socketed trend; no consumer yet
            // (narrative bark delivery is a separate ROADMAP item). NonLazy so the
            // subscription exists as soon as socketing does.
            Container.BindInterfacesAndSelfTo<SocketingTrendEvaluator>().AsSingle().NonLazy();
        }

        private void InstallVariantChoice()
        {
            // Every candidate part (built from the CharacterSystem part catalog).
            Container.BindInterfacesAndSelfTo<MutationPartCatalog>().AsSingle();

            // The adapter resolves the assembled character lazily so an unassembled rig just
            // rejects the request (no MonoBehaviour in Core). ModularCharacterVisual and the
            // body-plan coordinator are bound by CharacterSystemInstaller (their home system)
            // and resolved cross-installer here. Interfaces binding gives Zenject the
            // IDisposable lifecycle (the adapter unsubscribes from the coordinator).
            Container.BindInterfacesTo<ModularCharacterMutationAdapter>().AsSingle();

            // The card's mini-model popover: a hidden hero clone rendered to a RenderTexture at a
            // far world offset. Lazy - only resolved when the card view injects it.
            Container.Bind<IMutationModelPreview>()
                .To<MutationModelPreviewRig>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("MutationPreviewRig")
                .AsSingle();

            // The choice panel is instantiated from a prefab (mirrors InventoryInstaller's HUD view),
            // so it needs no scene authoring. If the prefab is missing the feature stays off and the
            // rest of the scene runs normally - never crash the SceneContext over an unbuilt panel.
            var panelPrefab = _choicePanelPrefab != null
                ? _choicePanelPrefab
                : Resources.Load<GameObject>(ChoicePanelResourcePath);
            if (panelPrefab == null)
            {
                Debug.LogWarning(
                    "[MutationInstaller] No MutationChoicePanel prefab assigned or found at " +
                    $"Resources/{ChoicePanelResourcePath}. Stage-up mutation choice is disabled this run.");
                return;
            }

            Container.Bind<IMutationChoiceView>()
                .To<MutationChoiceView>()
                .FromComponentInNewPrefab(panelPrefab)
                .AsSingle();

            // NonLazy so it subscribes to the blank-ready (unseal) signal at startup.
            Container.BindInterfacesAndSelfTo<MutationVariantPresenter>().AsSingle().NonLazy();
        }

        private MutationConfig LoadConfig()
        {
            if (_config != null)
            {
                return _config;
            }

            var loaded = Resources.Load<MutationConfig>(MutationConfigResourcePath);
            if (loaded == null)
            {
                throw new System.InvalidOperationException(
                    "[MutationInstaller] MutationConfig not assigned and none found at " +
                    $"Resources/{MutationConfigResourcePath}.");
            }

            Debug.Log($"[MutationInstaller] Auto-loaded MutationConfig from Resources/{MutationConfigResourcePath}");
            return loaded;
        }

        private List<PartBlankDefinition> LoadBlankDefinitions()
        {
            if (_blankDefinitions != null && _blankDefinitions.Count > 0)
            {
                return _blankDefinitions;
            }

            var loaded = new List<PartBlankDefinition>(
                Resources.LoadAll<PartBlankDefinition>(BlankDefinitionsResourcePath));
            if (loaded.Count > 0)
            {
                Debug.Log($"[MutationInstaller] Auto-loaded {loaded.Count} PartBlankDefinition " +
                          $"assets from Resources/{BlankDefinitionsResourcePath}");
            }
            else
            {
                Debug.LogWarning("[MutationInstaller] No PartBlankDefinition assets found in Inspector " +
                                 $"or Resources/{BlankDefinitionsResourcePath}");
            }

            return loaded;
        }

        private List<ArchetypeDefinition> LoadArchetypeDefinitions()
        {
            if (_archetypeDefinitions != null && _archetypeDefinitions.Count > 0)
            {
                return _archetypeDefinitions;
            }

            var loaded = new List<ArchetypeDefinition>(
                Resources.LoadAll<ArchetypeDefinition>(ArchetypeDefinitionsResourcePath));
            if (loaded.Count > 0)
            {
                Debug.Log($"[MutationInstaller] Auto-loaded {loaded.Count} ArchetypeDefinition " +
                          $"assets from Resources/{ArchetypeDefinitionsResourcePath}");
            }
            else
            {
                Debug.LogWarning("[MutationInstaller] No ArchetypeDefinition assets found in Inspector " +
                                 $"or Resources/{ArchetypeDefinitionsResourcePath}");
            }

            return loaded;
        }
    }
}
