using System.Collections.Generic;
using Mutation.Application;
using Mutation.Core;
using Mutation.Data;
using Mutation.Data.Definitions;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the mutation subsystem's data surface.
    /// Binds the archetype catalog (the authorable set of creature archetypes), the per-stage
    /// mutation tally, and a startup validator that warns about artifacts referencing unknown
    /// archetype ids. The feeding trigger that fills the tally and the stage-up mutation choice
    /// that resets it are not wired yet - see ROADMAP "M1".
    /// </summary>
    public class MutationInstaller : MonoInstaller
    {
        private const string ArchetypeDefinitionsResourcePath = "Mutation/Archetypes";
        private const string MutationConfigResourcePath = "Mutation/MutationConfig";

        [Header("Configuration (auto-loaded from Resources when empty)")]
        [SerializeField] private MutationConfig _config;

        [Header("Data Definitions (auto-loaded from Resources when empty)")]
        [SerializeField] private List<ArchetypeDefinition> _archetypeDefinitions;

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

            // NonLazy so the authoring validation always runs at startup.
            Container.BindInterfacesAndSelfTo<MutationContentValidator>().AsSingle().NonLazy();
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
    }
}
