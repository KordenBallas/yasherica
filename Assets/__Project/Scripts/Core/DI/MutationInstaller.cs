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
    /// Zenject installer for the mutation subsystem. Binds the archetype catalog (the authorable set
    /// of creature archetypes), the per-stage mutation tally and digestion progress, the stage-up
    /// mutation choice (option catalog + builder + presenter + view, with an adapter onto the live
    /// modular character), and a startup validator that warns about bad authoring. The feeding UI
    /// (InventoryInstaller) fills the tally; the stage-up choice consumes the ready signal and resets
    /// it. Part-derived ability grants are deferred - see ROADMAP "M1".
    /// </summary>
    public class MutationInstaller : MonoInstaller
    {
        private const string ArchetypeDefinitionsResourcePath = "Mutation/Archetypes";
        private const string PartSetDefinitionsResourcePath = "Mutation/PartSets";
        private const string MutationConfigResourcePath = "Mutation/MutationConfig";
        private const string ChoicePanelResourcePath = "Prefabs/UI/MutationChoicePanel";

        [Header("Configuration (auto-loaded from Resources when empty)")]
        [SerializeField] private MutationConfig _config;

        [Header("Data Definitions (auto-loaded from Resources when empty)")]
        [SerializeField] private List<ArchetypeDefinition> _archetypeDefinitions;
        [SerializeField] private List<ArchetypePartSetDefinition> _partSetDefinitions;

        [Header("Stage-up choice UI (auto-loaded from Resources when empty)")]
        [Tooltip("MutationChoicePanel prefab (built by Tools → Mutation → Setup Stage-Up Choice UI)")]
        [SerializeField] private GameObject _choicePanelPrefab;

        public override void InstallBindings()
        {
            // IGameLogger is provided by InventoryInstaller (same SceneContext, runs earlier).
            // Re-binding UnityGameLogger here would trip Zenject 6's "AsSingle multiple times for
            // the same concrete type" assert, even with IfNotBound.
            var archetypes = LoadArchetypeDefinitions();
            Container.Bind<IArchetypeCatalog>()
                .To<ArchetypeCatalog>()
                .AsSingle()
                .WithArguments(archetypes as IReadOnlyList<ArchetypeDefinition>);

            var config = LoadConfig();
            Container.BindInstance(config).AsSingle();

            // Per-stage tally shared by the feeding UI and the (future) stage-up mutation choice.
            // Not NonLazy: it has no startup side effect, created when its first consumer resolves.
            Container.BindInterfacesAndSelfTo<MutationTally>().AsSingle();

            // Per-stage digestion progress: how close the player is to a mutation this stage.
            // Threshold comes from authored data (no magic numbers - CLAUDE.md §11).
            Container.BindInterfacesAndSelfTo<DigestionProgress>()
                .AsSingle()
                .WithArguments(config.DigestionThreshold);

            var partSets = LoadPartSetDefinitions();
            InstallStageUpChoice(partSets);

            // NonLazy so the authoring validation always runs at startup. Also validates the part
            // sets, so it needs the raw definitions and the character part catalog (CharacterSystem).
            Container.BindInterfacesAndSelfTo<MutationContentValidator>()
                .AsSingle()
                .WithArguments(partSets as IReadOnlyList<ArchetypePartSetDefinition>)
                .NonLazy();
        }

        private void InstallStageUpChoice(List<ArchetypePartSetDefinition> partSets)
        {
            // Archetype -> body-part options, plus the deterministic builder that picks 2-3 of them.
            Container.BindInterfacesAndSelfTo<MutationOptionCatalog>()
                .AsSingle()
                .WithArguments(partSets as IReadOnlyList<ArchetypePartSetDefinition>);
            Container.Bind<IMutationOptionBuilder>().To<MutationOptionBuilder>().AsSingle();

            // The live character is a scene/prefab component; the adapter resolves its assembled
            // character lazily so an unassembled rig just fails the swap (no MonoBehaviour in Core).
            // Both stay lazy - they are only resolved when the presenter below binds.
            Container.Bind<ModularCharacterVisual>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IMutationCharacter>().To<ModularCharacterMutationAdapter>().AsSingle();

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
                    $"Resources/{ChoicePanelResourcePath} (run Tools → Mutation → Setup Stage-Up " +
                    "Choice UI). Stage-up mutation choice is disabled this run.");
                return;
            }

            Container.Bind<IMutationChoiceView>()
                .To<MutationChoiceView>()
                .FromComponentInNewPrefab(panelPrefab)
                .AsSingle();

            // NonLazy so it subscribes to the digestion ready signal at startup (mirrors FeedingPresenter).
            Container.BindInterfacesAndSelfTo<MutationChoicePresenter>().AsSingle().NonLazy();
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

        private List<ArchetypePartSetDefinition> LoadPartSetDefinitions()
        {
            if (_partSetDefinitions != null && _partSetDefinitions.Count > 0)
            {
                return _partSetDefinitions;
            }

            var loaded = new List<ArchetypePartSetDefinition>(
                Resources.LoadAll<ArchetypePartSetDefinition>(PartSetDefinitionsResourcePath));
            if (loaded.Count > 0)
            {
                Debug.Log($"[MutationInstaller] Auto-loaded {loaded.Count} ArchetypePartSetDefinition " +
                          $"assets from Resources/{PartSetDefinitionsResourcePath}");
            }
            else
            {
                Debug.LogWarning("[MutationInstaller] No ArchetypePartSetDefinition assets found in " +
                                 $"Inspector or Resources/{PartSetDefinitionsResourcePath}; the stage-up " +
                                 "mutation choice will have no options to offer.");
            }

            return loaded;
        }
    }
}
