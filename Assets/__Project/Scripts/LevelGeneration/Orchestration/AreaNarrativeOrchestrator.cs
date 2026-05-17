using System;
using Narrative;
using Narrative.Generation;
using UnityEngine;

namespace LevelGeneration.Orchestration
{
    /// <summary>
    /// Orchestrates area narrative generation by coordinating scenario generation,
    /// narrative context population, narrative generation, and story-platform binding.
    /// Pure C# class following MVP pattern - no Unity dependencies except logging.
    /// </summary>
    public class AreaNarrativeOrchestrator : IAreaNarrativeOrchestrator
    {
        private readonly IScenarioGenerator _scenarioGenerator;
        private readonly INarrativeGenerator _narrativeGenerator;
        private readonly INarrativeContext _narrativeContext;
        private readonly IStoryPlatformDataBinder _storyPlatformDataBinder;
        private readonly IStoryStateProvider _storyStateProvider;

        public event Action<ScenarioData, GenerationResult> OnAreaGenerated;

        /// <summary>
        /// Creates a new area narrative orchestrator.
        /// </summary>
        /// <param name="scenarioGenerator">Generator for base scenario data</param>
        /// <param name="narrativeGenerator">Generator for narrative content</param>
        /// <param name="narrativeContext">Context for narrative generation</param>
        /// <param name="storyPlatformDataBinder">Binder for attaching stories to platforms</param>
        /// <param name="storyStateProvider">Provider for current story state</param>
        public AreaNarrativeOrchestrator(
            IScenarioGenerator scenarioGenerator,
            INarrativeGenerator narrativeGenerator,
            INarrativeContext narrativeContext,
            IStoryPlatformDataBinder storyPlatformDataBinder,
            IStoryStateProvider storyStateProvider)
        {
            _scenarioGenerator = scenarioGenerator ?? throw new ArgumentNullException(nameof(scenarioGenerator));
            _narrativeGenerator = narrativeGenerator ?? throw new ArgumentNullException(nameof(narrativeGenerator));
            _narrativeContext = narrativeContext ?? throw new ArgumentNullException(nameof(narrativeContext));
            _storyPlatformDataBinder = storyPlatformDataBinder ?? throw new ArgumentNullException(nameof(storyPlatformDataBinder));
            _storyStateProvider = storyStateProvider ?? throw new ArgumentNullException(nameof(storyStateProvider));
        }

        public ScenarioData GenerateAreaScenario(GameContext context)
        {
            if (context == null)
            {
                Debug.LogError("[AreaNarrativeOrchestrator] GameContext is null");
                return null;
            }

            Debug.Log("[AreaNarrativeOrchestrator] Starting area narrative generation pipeline");

            // Phase 1: Generate base scenario
            var scenarioData = _scenarioGenerator.GenerateScenario(context);
            if (scenarioData == null)
            {
                Debug.LogError("[AreaNarrativeOrchestrator] Scenario generation failed");
                return null;
            }

            Debug.Log($"[AreaNarrativeOrchestrator] Phase 1 - Scenario generated: " +
                     $"{scenarioData.RequiredPlatforms?.Count ?? 0} platforms, " +
                     $"Theme: {scenarioData.Theme}");

            // Phase 2: Populate narrative context from scenario and game context
            PopulateNarrativeContext(scenarioData, context);
            Debug.Log("[AreaNarrativeOrchestrator] Phase 2 - Narrative context populated");

            // Phase 3: Generate narrative content (stories, NPCs, quests)
            GenerationResult generationResult = null;
            if (_narrativeGenerator.CanGenerate(_narrativeContext))
            {
                generationResult = _narrativeGenerator.GenerateForArea(_narrativeContext);

                if (generationResult != null && generationResult.Success)
                {
                    Debug.Log($"[AreaNarrativeOrchestrator] Phase 3 - Narrative generated: " +
                             $"{generationResult.StorySessions?.Count ?? 0} stories, " +
                             $"{generationResult.PlacedNpcs?.Count ?? 0} NPCs, " +
                             $"{generationResult.CreatedQuests?.Count ?? 0} quests");

                    // Log any warnings
                    if (generationResult.Warnings != null && generationResult.Warnings.Count > 0)
                    {
                        foreach (var warning in generationResult.Warnings)
                        {
                            Debug.LogWarning($"[AreaNarrativeOrchestrator] {warning}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[AreaNarrativeOrchestrator] Phase 3 - Narrative generation failed: " +
                                    $"{generationResult?.ErrorMessage ?? "Unknown error"}. " +
                                    "Continuing with platform-only data.");
                }
            }
            else
            {
                Debug.LogWarning("[AreaNarrativeOrchestrator] Phase 3 - Narrative generation skipped: " +
                                "generator reports cannot generate (missing templates or NPCs)");
            }

            // Phase 4: Bind generated stories to platform data
            if (generationResult != null && generationResult.Success)
            {
                _storyPlatformDataBinder.BindGeneratedStories(generationResult, scenarioData);
                Debug.Log("[AreaNarrativeOrchestrator] Phase 4 - Stories bound to platforms");
            }

            // Fire completion event
            OnAreaGenerated?.Invoke(scenarioData, generationResult);

            Debug.Log("[AreaNarrativeOrchestrator] Area narrative generation pipeline complete");
            return scenarioData;
        }

        /// <summary>
        /// Populates the narrative context from scenario data and game context.
        /// </summary>
        private void PopulateNarrativeContext(ScenarioData scenarioData, GameContext gameContext)
        {
            // Use the INarrativeContext method if available, otherwise set values directly
            if (_narrativeContext is NarrativeContext mutableContext)
            {
                mutableContext.PopulateFromScenario(scenarioData, gameContext);

                // Update world state from story state provider
                if (_storyStateProvider?.CurrentState != null)
                {
                    mutableContext.UpdateWorldState(_storyStateProvider.CurrentState);
                }
            }
            else
            {
                // Fallback: update through interface methods
                string areaId = scenarioData.ChapterId ?? Guid.NewGuid().ToString();
                _narrativeContext.UpdateArea(areaId, scenarioData.Theme, scenarioData.EstimatedPlatformCount);

                if (_storyStateProvider?.CurrentState != null)
                {
                    _narrativeContext.UpdateWorldState(_storyStateProvider.CurrentState);
                }
            }
        }
    }
}
