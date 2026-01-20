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

        [Header("Story Graph Configuration")]
        [Tooltip("Enable story graph system for advanced narrative features")]
        [SerializeField] private bool _enableStoryGraph = true;

        public override void InstallBindings()
        {
            InstallStoryManagement();
            InstallNpcDataProvider();
            InstallDialogueSystem();
            InstallSideStorySystem();

            // NEW: Install story graph system if enabled
            if (_enableStoryGraph)
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
            // Dialogue presenter (coordinates between model, view, and story)
            Container.Bind<DialoguePresenter>()
                .AsSingle();

            // Dialogue view binding
            if (_dialogueView != null)
            {
                // Use existing scene view
                Container.Bind<IDialogueView>()
                    .FromInstance(_dialogueView)
                    .AsSingle();
            }
            else if (_dialogueViewPrefab != null)
            {
                // Create view from prefab
                Container.Bind<IDialogueView>()
                    .To<DialogueView>()
                    .FromComponentInNewPrefab(_dialogueViewPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                Debug.LogWarning("[NarrativeInstaller] No dialogue view assigned - dialogue UI will not be available");
            }
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
            if (_enableStoryGraph)
            {
                // NEW: Story graph scenario generator (uses graph and selection strategies)
                Container.Bind<IScenarioGenerator>()
                    .To<StoryGraphScenarioGenerator>()
                    .AsSingle();

                Debug.Log("[NarrativeInstaller] Using StoryGraphScenarioGenerator for enhanced narrative generation");
            }
            else
            {
                // LEGACY: Story-aware scenario generator (old system)
                Container.Bind<IScenarioGenerator>()
                    .To<StoryAwareScenarioGenerator>()
                    .AsSingle();

                Debug.Log("[NarrativeInstaller] Using legacy StoryAwareScenarioGenerator");
            }
        }
    }
}
