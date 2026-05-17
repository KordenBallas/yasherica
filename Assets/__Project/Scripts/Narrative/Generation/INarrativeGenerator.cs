using System;
using System.Collections.Generic;

namespace Narrative.Generation
{
    /// <summary>
    /// Interface for orchestrating the narrative generation pipeline.
    /// Coordinates NPC pool, template selection, parameter binding, and quest creation.
    /// </summary>
    public interface INarrativeGenerator
    {
        /// <summary>
        /// Event fired when generation starts.
        /// </summary>
        event Action OnGenerationStarted;

        /// <summary>
        /// Event fired when generation completes.
        /// </summary>
        event Action<GenerationResult> OnGenerationCompleted;

        /// <summary>
        /// Event fired when a story session is created.
        /// </summary>
        event Action<StorySession> OnStorySessionCreated;

        /// <summary>
        /// Generates narrative content for a new area.
        /// </summary>
        /// <param name="context">The narrative context for generation</param>
        /// <returns>Result containing generated stories and NPCs</returns>
        GenerationResult GenerateForArea(INarrativeContext context);

        /// <summary>
        /// Generates a single story for the current platform.
        /// </summary>
        /// <param name="context">The narrative context</param>
        /// <param name="storyType">Type of story to generate (main/side)</param>
        /// <returns>The generated story session or null if none available</returns>
        StorySession GenerateSingleStory(INarrativeContext context, Data.Definitions.StoryType storyType);

        /// <summary>
        /// Validates the current context can support generation.
        /// </summary>
        /// <param name="context">The narrative context to validate</param>
        /// <returns>True if generation can proceed</returns>
        bool CanGenerate(INarrativeContext context);

        /// <summary>
        /// Gets the current generation statistics.
        /// </summary>
        GenerationStatistics GetStatistics();

        /// <summary>
        /// Resets the generator state for a new run.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Result of narrative generation.
    /// </summary>
    public class GenerationResult
    {
        /// <summary>
        /// Whether generation was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Generated story sessions.
        /// </summary>
        public IReadOnlyList<StorySession> StorySessions { get; }

        /// <summary>
        /// NPC instances placed in the area.
        /// </summary>
        public IReadOnlyList<NpcInstance> PlacedNpcs { get; }

        /// <summary>
        /// Created quests.
        /// </summary>
        public IReadOnlyList<QuestInstance> CreatedQuests { get; }

        /// <summary>
        /// Error message if generation failed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Warnings generated during generation.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        private GenerationResult(
            bool success,
            IReadOnlyList<StorySession> storySessions,
            IReadOnlyList<NpcInstance> placedNpcs,
            IReadOnlyList<QuestInstance> createdQuests,
            string errorMessage,
            IReadOnlyList<string> warnings)
        {
            Success = success;
            StorySessions = storySessions ?? Array.Empty<StorySession>();
            PlacedNpcs = placedNpcs ?? Array.Empty<NpcInstance>();
            CreatedQuests = createdQuests ?? Array.Empty<QuestInstance>();
            ErrorMessage = errorMessage;
            Warnings = warnings ?? Array.Empty<string>();
        }

        public static GenerationResult Succeeded(
            IReadOnlyList<StorySession> storySessions,
            IReadOnlyList<NpcInstance> placedNpcs,
            IReadOnlyList<QuestInstance> createdQuests,
            IReadOnlyList<string> warnings = null)
        {
            return new GenerationResult(true, storySessions, placedNpcs, createdQuests, null, warnings);
        }

        public static GenerationResult Failed(string errorMessage)
        {
            return new GenerationResult(false, null, null, null, errorMessage, null);
        }
    }

    /// <summary>
    /// Statistics about the generation process.
    /// </summary>
    public class GenerationStatistics
    {
        /// <summary>
        /// Total areas generated.
        /// </summary>
        public int AreasGenerated { get; set; }

        /// <summary>
        /// Total stories generated.
        /// </summary>
        public int StoriesGenerated { get; set; }

        /// <summary>
        /// Total NPCs placed.
        /// </summary>
        public int NpcsPlaced { get; set; }

        /// <summary>
        /// Total quests created.
        /// </summary>
        public int QuestsCreated { get; set; }

        /// <summary>
        /// Templates that failed binding.
        /// </summary>
        public int BindingFailures { get; set; }

        /// <summary>
        /// Main story generation failures (critical).
        /// </summary>
        public int MainStoryFailures { get; set; }

        /// <summary>
        /// Side story generation failures (non-critical).
        /// </summary>
        public int SideStoryFailures { get; set; }

        /// <summary>
        /// Average generation time in milliseconds.
        /// </summary>
        public float AverageGenerationTimeMs { get; set; }
    }
}
