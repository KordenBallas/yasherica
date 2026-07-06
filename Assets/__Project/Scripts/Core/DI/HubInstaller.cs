using System.Collections.Generic;
using Character;
using Hub.Core;
using Hub.Data;
using Hub.Presenter;
using Hub.View;
using Mutation.Data;
using Mutation.Data.Definitions;
using Mutation.Infrastructure;
using Mutation.View;
using UnityEngine;
using World.Races.Core;
using World.Races.Data;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the Hub scene (O1, hub-staging.md — reworked to the walkable
    /// junkyard platform): binds the persistence stores (the disk is the cross-scene carrier),
    /// the starting-part offer stack (tasted pool → selector → the SHARED mutation card panel,
    /// dealt by TALKING to the junk-keeper), the world assembly (standalone hex platform +
    /// keeper NPC + one labelled launch portal per homeland, all walk-up F-spots), the
    /// data-authored cauldron voice, and the presenter pairs. The scene's SceneContext also
    /// carries <see cref="CharacterSystemInstaller"/> (part catalog + the scene hero, who walks
    /// the platform). Deliberately absent: MutationInstaller (its blank rack would drag the whole
    /// inventory fusion graph in), the narrative slice, combat — the Hub reads meta.json
    /// snapshots directly, the Arena precedent.
    /// </summary>
    public class HubInstaller : MonoInstaller
    {
        private const string RacesResourcePath = "World/Races";
        private const string MutationConfigResourcePath = "Mutation/MutationConfig";
        private const string ChoicePanelResourcePath = "Prefabs/UI/MutationChoicePanel";
        private const string VoiceLinesResourcePath = "Hub/HubVoiceLines";
        private const string SceneConfigResourcePath = "Hub/HubSceneConfig";
        private const string PlatformShapeResourcePath = "LevelGeneration/PlatformShapeConfig";

        /// <summary>The staging presenter must deal the offer + homelands before the world
        /// entrypoint raises the portals and the voice presenter reads the offer.</summary>
        private const int StagingPresenterExecutionOrder = -10;

        /// <summary>The world entrypoint registers the F-spots after staging, before default order.</summary>
        private const int SceneEntrypointExecutionOrder = -5;

        [Header("Configuration (auto-loaded from Resources when empty)")]
        [Tooltip("The race roster; drives the portal homelands and the card tints.")]
        [SerializeField] private List<RaceDefinition> _raceDefinitions;

        [Tooltip("Cauldron-voice line pools; missing = a quiet cauldron.")]
        [SerializeField] private HubVoiceLinesConfig _voiceLines;

        [Tooltip("Hub world dressing (platform material/seed, keeper name, interaction radii).")]
        [SerializeField] private HubSceneConfig _sceneConfig;

        [Tooltip("MutationChoicePanel prefab (the shared card-hand panel under Resources/Prefabs/UI).")]
        [SerializeField] private GameObject _choicePanelPrefab;

        public override void InstallBindings()
        {
            LoggingInstaller.Install(Container);
            PersistenceInstaller.Install(Container);
            Container.Bind<Core.SceneFlow.ISceneLoader>().To<Core.SceneFlow.SceneLoader>().AsSingle();

            // The scene Hero prefab's components inject the registry (Area/Arena precedent).
            Container.Bind<ICharacterRegistry>()
                .To<CharacterRegistry>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();

            InstallPartCards();
            InstallStagingDomain();
            InstallVoice();
            InstallWorld();

            // MVP pairs. The staging presenter deals the offer + homelands; the world entrypoint
            // and the voice presenter read them on init, so the order is pinned explicitly.
            Container.Bind<HubStagingModel>().AsSingle();
            Container.Bind<IHubStagingView>().To<HubStagingView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IHubVoiceView>().To<HubVoicePlaqueView>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<HubStagingPresenter>().AsSingle().NonLazy();
            Container.BindExecutionOrder<HubStagingPresenter>(StagingPresenterExecutionOrder);
            Container.BindInterfacesTo<CauldronVoicePresenter>().AsSingle().NonLazy();
        }

        private void InstallWorld()
        {
            // The walkable junkyard island (O1 rework): the shared platform shape dials + the
            // Hub's own dressing config feed the standalone platform builder; the entrypoint
            // assembles the world (platform, keeper NPC, labelled portals) and registers the
            // F-spots on the proximity presenter, which ticks the walk-up prompts.
            var shapeConfig = Resources.Load<LevelGeneration.Data.PlatformShapeConfig>(PlatformShapeResourcePath);
            Container.Bind<LevelGeneration.Surface.PlatformShapeSettings>()
                .FromInstance(LevelGeneration.Data.PlatformShapeConfigMapper.ToSettings(shapeConfig))
                .AsSingle();

            var sceneConfig = _sceneConfig != null
                ? _sceneConfig
                : Resources.Load<HubSceneConfig>(SceneConfigResourcePath);
            if (sceneConfig == null)
            {
                Debug.LogWarning("[HubInstaller] No HubSceneConfig assigned or found at " +
                                 $"Resources/{SceneConfigResourcePath}; using code defaults.");
                sceneConfig = ScriptableObject.CreateInstance<HubSceneConfig>();
            }

            Container.BindInstance(sceneConfig).AsSingle();
            Container.Bind<Hub.View.HubPlatformBuilder>().AsSingle();

            Container.Bind<Narrative.Interaction.IInteractionInput>()
                .To<Narrative.Interaction.View.NpcInteractionInput>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("HubInteractionInput")
                .AsSingle();
            Container.BindInterfacesAndSelfTo<HubProximityPresenter>().AsSingle();

            Container.BindInterfacesTo<Hub.View.HubSceneEntrypoint>().AsSingle().NonLazy();
            Container.BindExecutionOrder<Hub.View.HubSceneEntrypoint>(SceneEntrypointExecutionOrder);
        }

        private void InstallPartCards()
        {
            // The shared mutation card stack (mutation-choice-cards.md), reused as the Hub's
            // starting-part offer: catalog card faces, the mini-model preview rig, and the shared
            // ability-preview popover. IPartCatalog / the hero visual / the factory come from
            // CharacterSystemInstaller on this same SceneContext.
            Container.Bind<Combat.Integration.IPartAbilityResolver>()
                .To<Combat.Integration.PartAbilityResolver>()
                .AsSingle();
            Container.BindInterfacesAndSelfTo<MutationPartCatalog>().AsSingle();

            var config = Resources.Load<MutationConfig>(MutationConfigResourcePath);
            if (config != null)
            {
                Container.BindInstance(config).AsSingle();
            }
            else
            {
                Debug.LogWarning("[HubInstaller] MutationConfig not found at " +
                                 $"Resources/{MutationConfigResourcePath}; card previews use defaults.");
            }

            Container.Bind<IMutationModelPreview>()
                .To<MutationModelPreviewRig>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("MutationPreviewRig")
                .AsSingle();
            Container.Bind<UI.AbilityPreview.IAbilityPreviewHeroSource>()
                .To<MutationAbilityPreviewHeroSource>()
                .AsSingle();
            AbilityPreviewInstaller.Install(Container);

            // Missing prefab → the binding is skipped and the presenter's optional param tolerates
            // it (a bare-launch-only Hub) — never a crashed SceneContext (MutationInstaller guard).
            var panelPrefab = _choicePanelPrefab != null
                ? _choicePanelPrefab
                : Resources.Load<GameObject>(ChoicePanelResourcePath);
            if (panelPrefab != null)
            {
                Container.Bind<IMutationChoiceView>()
                    .To<MutationChoiceView>()
                    .FromComponentInNewPrefab(panelPrefab)
                    .AsSingle();
            }
            else
            {
                Debug.LogWarning("[HubInstaller] No MutationChoicePanel prefab found at " +
                                 $"Resources/{ChoicePanelResourcePath}; the Hub is bare-launch-only.");
            }
        }

        private void InstallStagingDomain()
        {
            // The tasted-forms read is the Arena's reader, reused as-is (relocation = ROADMAP debt).
            Container.Bind<Combat.Arena.Data.ArenaTastedCatalogReader>().AsSingle();
            Container.Bind<IStartingPartPoolSource>().To<HubStartingPoolSource>().AsSingle();
            Container.Bind<StartingPartSelector>().AsSingle();
            Container.Bind<HubMetaReader>().AsSingle();

            var races = _raceDefinitions;
            if (races == null || races.Count == 0)
            {
                races = new List<RaceDefinition>(Resources.LoadAll<RaceDefinition>(RacesResourcePath));
            }

            var captured = races;
            Container.Bind<IRaceRoster>()
                .FromMethod(ctx => RaceRosterMapper.ToRoster(
                    captured, ctx.Container.Resolve<Core.Logging.IGameLogger>()))
                .AsSingle();
            Container.Bind<IRaceTintCatalog>()
                .To<RaceTintCatalog>()
                .AsSingle()
                .WithArguments(captured as IReadOnlyList<RaceDefinition>);
        }

        private void InstallVoice()
        {
            var voiceConfig = _voiceLines != null
                ? _voiceLines
                : Resources.Load<HubVoiceLinesConfig>(VoiceLinesResourcePath);
            Container.Bind<CauldronVoiceLines>()
                .FromMethod(ctx => HubVoiceLinesMapper.ToLines(
                    voiceConfig, ctx.Container.Resolve<Core.Logging.IGameLogger>()))
                .AsSingle();
        }
    }
}
