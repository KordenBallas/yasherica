using System;
using System.Collections.Generic;
using System.Diagnostics;
using Narrative.Data.Definitions;
using Debug = UnityEngine.Debug;

// IStoryManager is in Narrative namespace (not Generation)

namespace Narrative.Generation
{
    /// <summary>
    /// Orchestrates the narrative generation pipeline.
    /// Coordinates NPC pool, template selection, parameter binding, and quest creation.
    /// Applies different policies for Main Story vs Side Story generation.
    /// Pure C# class - no Unity dependencies except logging.
    /// </summary>
    public class NarrativeGenerator : INarrativeGenerator
    {
        private readonly INpcPool _npcPool;
        private readonly IStoryTemplateSelector _templateSelector;
        private readonly IParameterBinder _parameterBinder;
        private readonly IQuestManager _questManager;
        private readonly IStoryManager _storyManager;
        private readonly IStoryPolicyProvider _policyProvider;
        private readonly IReadOnlyList<NpcDefinition> _npcDefinitions;

        private GenerationStatistics _statistics;
        private List<long> _generationTimes;

        public event Action OnGenerationStarted;
        public event Action<GenerationResult> OnGenerationCompleted;
        public event Action<StorySession> OnStorySessionCreated;

        /// <summary>
        /// Creates a new narrative generator.
        /// </summary>
        public NarrativeGenerator(
            INpcPool npcPool,
            IStoryTemplateSelector templateSelector,
            IParameterBinder parameterBinder,
            IQuestManager questManager,
            IStoryManager storyManager,
            IStoryPolicyProvider policyProvider,
            IReadOnlyList<NpcDefinition> npcDefinitions = null)
        {
            _npcPool = npcPool ?? throw new ArgumentNullException(nameof(npcPool));
            _templateSelector = templateSelector ?? throw new ArgumentNullException(nameof(templateSelector));
            _parameterBinder = parameterBinder ?? throw new ArgumentNullException(nameof(parameterBinder));
            _questManager = questManager ?? throw new ArgumentNullException(nameof(questManager));
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
            _policyProvider = policyProvider ?? new StoryPolicyProvider();
            _npcDefinitions = npcDefinitions ?? Array.Empty<NpcDefinition>();

            _statistics = new GenerationStatistics();
            _generationTimes = new List<long>();
        }

        public GenerationResult GenerateForArea(INarrativeContext context)
        {
            if (context == null)
                return GenerationResult.Failed("Context is null");

            var stopwatch = Stopwatch.StartNew();
            OnGenerationStarted?.Invoke();

            var warnings = new List<string>();
            var storySessions = new List<StorySession>();
            var placedNpcs = new List<NpcInstance>();
            var createdQuests = new List<QuestInstance>();

            try
            {
                // Phase 1: Populate NPC pool
                _npcPool.PopulatePool(_npcDefinitions, context);
                Debug.Log($"[NarrativeGenerator] Phase 1 - NPC pool populated with {_npcPool.AllNpcs.Count} NPCs");

                // Phase 2: Decrement cooldowns from previous area
                _npcPool.DecrementCooldowns();

                // Phase 3: Determine story counts using policy provider
                var mainPolicy = _policyProvider.MainStoryPolicy;
                var sidePolicy = _policyProvider.SideStoryPolicy;

                int mainStoryCount = mainPolicy.RequiredCountPerArea; // Always 1 per policy
                int sideStoryCount = _policyProvider.CalculateSideStoryCount(context.TotalPlatforms);
                int totalStories = mainStoryCount + sideStoryCount;

                Debug.Log($"[NarrativeGenerator] Story counts - Main: {mainStoryCount} (required), Side: {sideStoryCount} (based on {context.TotalPlatforms} platforms)");

                // Phase 3: Generate main story (FIRST - gets primary NPC pool access)
                Debug.Log("[NarrativeGenerator] Phase 3 - Generating main story (primary NPC reservation)");
                var mainSession = GenerateSingleStory(context, StoryType.Chapter);

                if (mainSession != null)
                {
                    storySessions.Add(mainSession);
                    OnStorySessionCreated?.Invoke(mainSession);
                    Debug.Log($"[NarrativeGenerator] Main story generated: {mainSession.BoundStory?.Template?.DisplayName}");
                }
                else
                {
                    // Main story failure - apply policy-based handling
                    LogGenerationFailure(StoryType.Chapter, "Failed to generate main story", mainPolicy);

                    if (mainPolicy.IsFailureCritical && !mainPolicy.ContinueOnFailure)
                    {
                        // Critical failure - return early with warning
                        warnings.Add("CRITICAL: Failed to generate main story - narrative spine missing");
                        _statistics.MainStoryFailures++;

                        stopwatch.Stop();
                        var criticalResult = GenerationResult.Succeeded(storySessions, placedNpcs, createdQuests, warnings);
                        OnGenerationCompleted?.Invoke(criticalResult);
                        return criticalResult;
                    }

                    warnings.Add("Failed to generate main story");
                    _statistics.MainStoryFailures++;
                }

                // Phase 4: Generate side stories (AFTER main story - uses remaining NPC pool)
                Debug.Log($"[NarrativeGenerator] Phase 4 - Generating {sideStoryCount} side stories (secondary NPC reservation)");

                for (int i = 0; i < sideStoryCount; i++)
                {
                    var sideSession = GenerateSingleStory(context, StoryType.SideStory);

                    if (sideSession != null)
                    {
                        storySessions.Add(sideSession);
                        OnStorySessionCreated?.Invoke(sideSession);
                        Debug.Log($"[NarrativeGenerator] Side story {i + 1} generated: {sideSession.BoundStory?.Template?.DisplayName}");
                    }
                    else
                    {
                        // Side story failure - apply policy-based handling (non-critical, continue)
                        LogGenerationFailure(StoryType.SideStory, $"Failed to generate side story {i + 1}", sidePolicy);
                        warnings.Add($"Failed to generate side story {i + 1}");
                        _statistics.SideStoryFailures++;

                        // Policy: ContinueOnFailure = true, so loop continues to next
                    }
                }

                // Collect placed NPCs and quests from sessions
                foreach (var session in storySessions)
                {
                    if (session.BoundStory?.BoundNpcs != null)
                    {
                        placedNpcs.AddRange(session.BoundStory.BoundNpcs);
                    }

                    if (session.AssociatedQuest != null)
                    {
                        createdQuests.Add(session.AssociatedQuest);
                    }
                }

                // Update statistics
                stopwatch.Stop();
                UpdateStatistics(storySessions.Count, placedNpcs.Count, createdQuests.Count, stopwatch.ElapsedMilliseconds);

                var result = GenerationResult.Succeeded(storySessions, placedNpcs, createdQuests, warnings);
                OnGenerationCompleted?.Invoke(result);

                Debug.Log($"[NarrativeGenerator] Generation complete: {storySessions.Count} stories " +
                         $"({CountByType(storySessions, StoryType.Chapter)} main, {CountByType(storySessions, StoryType.SideStory)} side), " +
                         $"{placedNpcs.Count} NPCs, {createdQuests.Count} quests in {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"[NarrativeGenerator] Generation failed with exception: {ex.Message}");
                return GenerationResult.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Logs a generation failure with the appropriate log level based on policy.
        /// </summary>
        private void LogGenerationFailure(StoryType storyType, string message, StoryPolicy policy)
        {
            string fullMessage = $"[NarrativeGenerator] {storyType}: {message}";

            switch (policy.FailureLogLevel)
            {
                case FailureLogLevel.Critical:
                    Debug.LogError($"CRITICAL - {fullMessage}");
                    break;
                case FailureLogLevel.Error:
                    Debug.LogError(fullMessage);
                    break;
                case FailureLogLevel.Warning:
                default:
                    Debug.LogWarning(fullMessage);
                    break;
            }
        }

        /// <summary>
        /// Counts story sessions by type.
        /// </summary>
        private static int CountByType(List<StorySession> sessions, StoryType type)
        {
            int count = 0;
            foreach (var session in sessions)
            {
                if (session.BoundStory?.Template?.StoryType == type)
                    count++;
            }
            return count;
        }

        public StorySession GenerateSingleStory(INarrativeContext context, StoryType storyType)
        {
            if (context == null)
                return null;

            // Get policy for this story type
            var policy = _policyProvider.GetPolicy(storyType);

            // Update context with available NPCs
            if (context is NarrativeContext mutableContext)
            {
                mutableContext.SetAvailableNpcs(_npcPool.AvailableNpcs);
            }

            // Step 1: Select template (filtered by story type)
            var selection = SelectBestTemplateForType(context, storyType);
            if (selection == null)
            {
                LogGenerationFailure(storyType, $"No eligible template found for {storyType}", policy);
                return null;
            }

            Debug.Log($"[NarrativeGenerator] Selected {storyType} template: {selection.Template.DisplayName} (score: {selection.Score:F1})");

            // Step 2: Validate parameter binding is possible
            if (!_parameterBinder.CanBindAllParameters(selection, context, _npcPool))
            {
                LogGenerationFailure(storyType, $"Cannot bind all parameters for template: {selection.Template.StoryId}", policy);
                _statistics.BindingFailures++;
                return null;
            }

            // Step 3: Bind parameters (NPC reservation happens here)
            var boundStory = _parameterBinder.BindParameters(selection, context, _npcPool);
            if (boundStory == null)
            {
                LogGenerationFailure(storyType, $"Parameter binding failed for template: {selection.Template.StoryId}", policy);
                _statistics.BindingFailures++;
                return null;
            }

            // Step 4: Create story session
            var session = new StorySession(boundStory, _storyManager);

            // Step 5: Create associated quest
            var quest = _questManager.CreateQuest(boundStory);
            if (quest != null)
            {
                session.SetAssociatedQuest(quest);
            }

            // Step 6: Set cooldowns on used NPCs (policy-based multiplier)
            int baseCooldown = selection.Template.CooldownPlatforms;
            int adjustedCooldown = (int)(baseCooldown * policy.CooldownMultiplier);

            foreach (var npc in boundStory.BoundNpcs)
            {
                _npcPool.SetCooldown(npc.InstanceId, adjustedCooldown);
            }

            // Step 7: Record story as recent
            context.RecordCompletedStory(selection.Template.StoryId);

            return session;
        }

        /// <summary>
        /// Selects the best template for a specific story type.
        /// Uses the StoryTemplateSelector with type filtering.
        /// </summary>
        private StoryTemplateSelection SelectBestTemplateForType(INarrativeContext context, StoryType storyType)
        {
            // Use the interface method with type filtering
            return _templateSelector.SelectBestTemplate(context, storyType);
        }

        public bool CanGenerate(INarrativeContext context)
        {
            if (context == null)
                return false;

            // Check if we have NPC definitions
            if (_npcDefinitions == null || _npcDefinitions.Count == 0)
            {
                Debug.LogWarning("[NarrativeGenerator] No NPC definitions available");
                return false;
            }

            // Check if template selector has templates
            var templates = _templateSelector.GetTemplatesByType(StoryType.SideStory);
            if (templates == null || templates.Count == 0)
            {
                Debug.LogWarning("[NarrativeGenerator] No story templates available");
                return false;
            }

            return true;
        }

        public GenerationStatistics GetStatistics()
        {
            return _statistics;
        }

        public void Reset()
        {
            _npcPool.ClearPool();
            _statistics = new GenerationStatistics();
            _generationTimes.Clear();
        }

        private void UpdateStatistics(int stories, int npcs, int quests, long timeMs)
        {
            _statistics.AreasGenerated++;
            _statistics.StoriesGenerated += stories;
            _statistics.NpcsPlaced += npcs;
            _statistics.QuestsCreated += quests;

            _generationTimes.Add(timeMs);

            // Calculate average
            long totalTime = 0;
            foreach (var time in _generationTimes)
            {
                totalTime += time;
            }
            _statistics.AverageGenerationTimeMs = totalTime / (float)_generationTimes.Count;
        }
    }
}
