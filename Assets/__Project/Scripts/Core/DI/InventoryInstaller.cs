using System.Collections.Generic;
using Character;
using Core.Logging;
using Inventory.Core;
using Inventory.Data;
using Inventory.Data.Definitions;
using Inventory.Presenter;
using Inventory.View;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the magic pot inventory subsystem.
    /// Binds the pure-C# domain (inventory, crafting, recipes), artifact data,
    /// world-space pot views, the HUD, and both presenters.
    /// </summary>
    public class InventoryInstaller : MonoInstaller
    {
        private const string ArtifactDefinitionsResourcePath = "Artifacts/Definitions";
        private const string RecipeDefinitionsResourcePath = "Artifacts/Recipes";
        private const string TraitDefinitionsResourcePath = "Artifacts/Traits";
        private const string FusionRuleDefinitionsResourcePath = "Artifacts/FusionRules";
        private const string PartInventoryPanelResourcePath = "Prefabs/UI/PartInventoryPanel";

        [Header("Configuration")]
        [SerializeField] private InventoryConfig _config;

        [Header("Data Definitions (auto-loaded from Resources when empty)")]
        [SerializeField] private List<ArtifactDefinition> _artifactDefinitions;
        [SerializeField] private List<RecipeDefinition> _recipeDefinitions;
        [SerializeField] private List<TraitDefinition> _traitDefinitions;
        [SerializeField] private List<FusionRuleDefinition> _fusionRuleDefinitions;

        [Header("HUD")]
        [SerializeField] private InventoryHudView _hudView;
        [SerializeField] private InventoryHudView _hudViewPrefab;

        public override void InstallBindings()
        {
            ValidateConfiguration();

            // IGameLogger is provided by LoggingInstaller (installed by AreaInstaller); we only resolve it.
            InstallData();
            InstallDomain();
            InstallViews();
            InstallPresenters();
        }

        private void ValidateConfiguration()
        {
            if (_config == null)
            {
                throw new System.InvalidOperationException(
                    "[InventoryInstaller] InventoryConfig not assigned! " +
                    "Assign the config asset in the scene's InventoryInstaller component. " +
                    "The asset should be located in Resources/Configs/InventoryConfig.asset");
            }
        }

        private void InstallData()
        {
            Container.BindInstance(_config).AsSingle();

            var artifacts = LoadDefinitions(_artifactDefinitions, ArtifactDefinitionsResourcePath);
            Container.Bind<IArtifactCatalog>()
                .To<ArtifactCatalog>()
                .AsSingle()
                .WithArguments(artifacts as IReadOnlyList<ArtifactDefinition>);

            var recipes = LoadDefinitions(_recipeDefinitions, RecipeDefinitionsResourcePath);
            Container.Bind<RecipeBookBuilder>().AsSingle();
            Container.Bind<IRecipeBook>()
                .FromMethod(ctx => ctx.Container.Resolve<RecipeBookBuilder>()
                    .Build(recipes as IReadOnlyList<RecipeDefinition>))
                .AsSingle();

            var traits = LoadDefinitions(_traitDefinitions, TraitDefinitionsResourcePath);
            Container.Bind<ITraitCatalog>()
                .To<TraitCatalog>()
                .AsSingle()
                .WithArguments(traits as IReadOnlyList<TraitDefinition>);

            Container.Bind<IArtifactTraitSource>().To<ArtifactTraitIndex>().AsSingle();

            var fusionRules = LoadDefinitions(_fusionRuleDefinitions, FusionRuleDefinitionsResourcePath);
            Container.Bind<FusionRuleSetBuilder>().AsSingle();
            Container.Bind<TraitFusionRuleSet>()
                .FromMethod(ctx => ctx.Container.Resolve<FusionRuleSetBuilder>()
                    .Build(fusionRules as IReadOnlyList<FusionRuleDefinition>))
                .AsSingle();

            Container.BindInstance(new FusionSettings(
                _config.AmplifyTierBonus,
                _config.TraitOverlapWeight,
                _config.TraitMismatchWeight,
                _config.TierProximityWeight)).AsSingle();
        }

        private void InstallDomain()
        {
            Container.Bind<IInventoryModel>().To<InventoryModel>().AsSingle();
            Container.Bind<IPartInventoryModel>().To<PartInventoryModel>().AsSingle();
            Container.Bind<CharacterSystem.Core.IShedPartSink>()
                .To<Inventory.Integration.PartInventorySink>()
                .AsSingle();
            Container.Bind<EmergentFusionCalculator>().AsSingle();
            Container.Bind<ArtifactByTraitSelector>().AsSingle();
            Container.Bind<IFusionResolver>().To<FusionResolver>().AsSingle();
            Container.Bind<ICraftingSession>()
                .To<CraftingSession>()
                .AsSingle()
                .WithArguments(_config.ItemsToCombine);
            Container.Bind<BubbleLayoutCalculator>().AsSingle();
        }

        private void InstallViews()
        {
            InstallHudView();
            InstallPartInventoryView();

            // Pot and crafting views live on the detached inventory stage (scene object).
            Container.Bind<IPotView>()
                .To<PotView>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.Bind<ICraftingSlotsView>()
                .To<CraftingSlotsView>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.Bind<IInventoryStageView>()
                .To<InventoryStageView>()
                .FromComponentInHierarchy()
                .AsSingle();

            // Hero-side adapters: movement lock, forced facing, and the camera anchor.
            Container.Bind<IMovementInputLock>()
                .To<CharacterMovementController>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.Bind<ICharacterFacing>()
                .To<CharacterFacingController>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.Bind<Camera.IBellyAnchorProvider>()
                .To<Camera.BellyAnchorMarker>()
                .FromComponentInHierarchy()
                .AsSingle();
        }

        private void InstallHudView()
        {
            if (_hudView != null)
            {
                Container.Bind<IInventoryHudView>()
                    .FromInstance(_hudView)
                    .AsSingle();
            }
            else if (_hudViewPrefab != null)
            {
                Container.Bind<IInventoryHudView>()
                    .To<InventoryHudView>()
                    .FromComponentInNewPrefab(_hudViewPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                throw new System.InvalidOperationException(
                    "[InventoryInstaller] No HUD view assigned. " +
                    "Assign either _hudView (scene instance) or _hudViewPrefab.");
            }
        }

        private void InstallPartInventoryView()
        {
            // Stored-parts readout (parts shed by body-plan changes). Missing prefab degrades
            // gracefully: the stash still works, only the readout is absent (mirrors the
            // MutationInstaller guard convention).
            var panelPrefab = Resources.Load<PartInventoryView>(PartInventoryPanelResourcePath);
            if (panelPrefab == null)
            {
                Debug.LogWarning(
                    $"[InventoryInstaller] Part inventory panel prefab not found at Resources/{PartInventoryPanelResourcePath}; the stored-parts readout is disabled.");
                return;
            }

            Container.Bind<IPartInventoryView>()
                .To<PartInventoryView>()
                .FromComponentInNewPrefab(panelPrefab)
                .AsSingle()
                .NonLazy();

            Container.BindInterfacesAndSelfTo<PartInventoryPresenter>().AsSingle().NonLazy();
        }

        private void InstallPresenters()
        {
            // BindInterfacesTo gives Zenject IInitializable/IDisposable lifecycle control.
            Container.BindInterfacesAndSelfTo<InventoryPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<CraftingPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesTo<Inventory.Application.ArtifactContentValidator>().AsSingle().NonLazy();
        }

        private List<TDefinition> LoadDefinitions<TDefinition>(List<TDefinition> assigned, string resourcePath)
            where TDefinition : ScriptableObject
        {
            if (assigned != null && assigned.Count > 0)
            {
                return assigned;
            }

            var loaded = new List<TDefinition>(Resources.LoadAll<TDefinition>(resourcePath));
            if (loaded.Count > 0)
            {
                Debug.Log($"[InventoryInstaller] Auto-loaded {loaded.Count} {typeof(TDefinition).Name} assets from Resources/{resourcePath}");
            }
            else
            {
                Debug.LogWarning($"[InventoryInstaller] No {typeof(TDefinition).Name} assets found in Inspector or Resources/{resourcePath}");
            }

            return loaded;
        }
    }
}
