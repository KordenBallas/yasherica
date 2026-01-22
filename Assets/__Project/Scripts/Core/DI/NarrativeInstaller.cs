using System.Collections.Generic;
using System.Linq;
using Narrative;
using Narrative.Data.Definitions;
using Narrative.Data.Providers;
using Narrative.Dialogue;
using Narrative.Discovery;
using Narrative.Graph;
using Narrative.Providers;
using Narrative.Selection;
using Narrative.View;
using LevelGeneration;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the narrative system.
    /// Binds story management, dialogue, NPC services, and story graph system.
    /// </summary>
    public class NarrativeInstaller : MonoInstaller
    {
        [Header("Story Configuration")]
        [Tooltip("Chapter definitions for the story")]
        [SerializeField] private List<StoryChapterDefinition> _chapterDefinitions;

        [Header("NPC Data")]
        [Tooltip("NPC definitions for the data-driven system")]
        [SerializeField] private List<NpcDefinition> _npcDefinitions;

        [Header("Dialogue UI")]
        [Tooltip("Dialogue view component in scene (optional - can be created dynamically)")]
        [SerializeField] private DialogueView _dialogueView;

        [Tooltip("Dialogue view prefab for dynamic instantiation")]
        [SerializeField] private DialogueView _dialogueViewPrefab;

        [Header("Scenario Generator Configuration")]
        [Tooltip("Select which scenario generator to use")]
        [SerializeField] private ScenarioGeneratorType _generatorType = ScenarioGeneratorType.StoryGraph;

        public override void InstallBindings()
        {
            Debug.Log("[NarrativeInstaller] Install Bindings");
            InstallStoryManagement();
            InstallNpcDataProvider();
            InstallDialogueSystem();
            InstallExternalFunctionBinder();
            InstallSideStorySystem();

            // Install story graph system for generators that need it
            if (_generatorType == ScenarioGeneratorType.StoryGraph)
            {
                InstallStoryGraph();
                InstallNpcStoryProvider();
            }

            InstallScenarioGeneration();
        }

        private void InstallStoryManagement()
        {
            // Core story manager (Ink wrapper)
            Container.Bind<IStoryManager>()
                .To<InkStoryManager>()
                .AsSingle();

            // Story requirement parser
            Container.Bind<IStoryRequirementParser>()
                .To<InkTagStoryRequirementParser>()
                .AsSingle();

            // Story state provider
            Container.Bind<IStoryStateProvider>()
                .To<StoryStateProvider>()
                .AsSingle()
                .WithArguments(_chapterDefinitions as IReadOnlyList<StoryChapterDefinition>);
        }

        private void InstallNpcDataProvider()
        {
            if (_npcDefinitions != null && _npcDefinitions.Count > 0)
            {
                Container.Bind<INpcDataProvider>()
                    .To<ScriptableObjectNpcDataProvider>()
                    .AsSingle()
                    .WithArguments(_npcDefinitions as IReadOnlyList<NpcDefinition>);
            }
            else
            {
                // Bind empty provider if no definitions
                Container.Bind<INpcDataProvider>()
                    .To<ScriptableObjectNpcDataProvider>()
                    .AsSingle()
                    .WithArguments(new List<NpcDefinition>() as IReadOnlyList<NpcDefinition>);

                Debug.LogWarning("[NarrativeInstaller] No NPC definitions assigned - using empty provider");
            }
        }

        private void InstallDialogueSystem()
        {
            // Dialogue view binding (MUST be first so presenter can use it)
            if (_dialogueView != null)
            {
                Container.Bind<IDialogueView>()
                    .FromInstance(_dialogueView)
                    .AsSingle();
            }
            else if (_dialogueViewPrefab != null)
            {
                Container.Bind<IDialogueView>()
                    .To<DialogueView>()
                    .FromComponentInNewPrefab(_dialogueViewPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                throw new System.InvalidOperationException(
                    "[NarrativeInstaller] No dialogue view assigned! " +
                    "Assign either _dialogueView or _dialogueViewPrefab in the inspector. " +
                    "DialoguePresenter requires a view to function.");
            }

            // Dialogue presenter - will automatically receive IDialogueView via constructor injection
            Container.Bind<DialoguePresenter>()
                .AsSingle();
        }

        private void InstallExternalFunctionBinder()
        {
            // External function binder for Ink external functions
            Container.Bind<IInkExternalFunctionBinder>()
                .To<InkExternalFunctionBinder>()
                .AsSingle();
        }

        private void InstallSideStorySystem()
        {
            // Side story discovery service (auto-discovers from Resources/SideStories)
            Container.Bind<ISideStoryDiscoveryService>()
                .To<ResourcesSideStoryDiscoveryService>()
                .AsSingle();

            // Prerequisite evaluator for unlocking side stories
            Container.Bind<SideStoryPrerequisiteEvaluator>()
                .AsSingle();

            // Side story provider (filters and selects available stories)
            Container.Bind<ISideStoryProvider>()
                .To<SideStoryProvider>()
                .AsSingle();
        }

        private void InstallStoryGraph()
        {
            // Collect all story definitions (chapters, side stories, dialogues)
            var allStories = new List<BaseStoryDefinition>();

            // Add chapters
            if (_chapterDefinitions != null)
            {
                allStories.AddRange(_chapterDefinitions);
            }

            // Add side stories (discovered from Resources)
            var sideStories = Resources.LoadAll<SideStoryDefinition>("SideStories");
            allStories.AddRange(sideStories);

            // Add dialogue stories (discovered from Resources)
            var dialogueStories = Resources.LoadAll<DialogueStoryDefinition>("Stories");
            allStories.AddRange(dialogueStories);

            Debug.Log($"[NarrativeInstaller] Story Graph initialized with {allStories.Count} stories " +
                      $"({_chapterDefinitions?.Count ?? 0} chapters, {sideStories.Length} side stories, {dialogueStories.Length} dialogues)");

            // Bind story graph provider
            Container.Bind<IStoryGraphProvider>()
                .To<StoryGraphProvider>()
                .AsSingle()
                .WithArguments(allStories as IReadOnlyList<BaseStoryDefinition>);

            // Bind story selection strategy (default: Balanced)
            Container.Bind<IStorySelectionStrategy>()
                .To<BalancedStoryStrategy>()
                .AsSingle();
        }

        private void InstallNpcStoryProvider()
        {
            // NPC story provider for dynamic NPC dialogue selection
            Container.Bind<INpcStoryProvider>()
                .To<NpcStoryProvider>()
                .AsSingle()
                .WithArguments(_npcDefinitions as IReadOnlyList<NpcDefinition>);
        }

        private void InstallScenarioGeneration()
        {
            Debug.Log("[NarrativeInstaller] Binding ScenarioGenerator");
            switch (_generatorType)
            {
                case ScenarioGeneratorType.StoryGraph:
                    Container.Bind<IScenarioGenerator>()
                        .To<StoryGraphScenarioGenerator>()
                        .AsSingle();
                    Debug.Log("[NarrativeInstaller] Using StoryGraphScenarioGenerator");
                    break;

                case ScenarioGeneratorType.StoryAware:
                    Container.Bind<IScenarioGenerator>()
                        .To<StoryAwareScenarioGenerator>()
                        .AsSingle();
                    Debug.Log("[NarrativeInstaller] Using StoryAwareScenarioGenerator");
                    break;

                case ScenarioGeneratorType.NpcSequence:
                    Container.Bind<IScenarioGenerator>()
                        .To<NpcSequenceScenarioGenerator>()
                        .AsSingle();
                    Debug.Log("[NarrativeInstaller] Using NpcSequenceScenarioGenerator");
                    break;
            }
        }
    }

    /// <summary>
    /// Available scenario generator types.
    /// </summary>
    public enum ScenarioGeneratorType
    {
        /// <summary>
        /// Uses story graph and selection strategies for advanced narrative generation.
        /// </summary>
        StoryGraph,

        /// <summary>
        /// Uses story-aware generation with Ink requirements (legacy).
        /// </summary>
        StoryAware,

        /// <summary>
        /// Generates sequential NPC encounters with empty platforms between each.
        /// </summary>
        NpcSequence
    }
}
