using System;
using Narrative.Generation;

namespace LevelGeneration.Orchestration
{
    /// <summary>
    /// Interface for orchestrating area narrative generation.
    /// Coordinates scenario generation, narrative context population,
    /// narrative generation, and story-platform binding into a unified flow.
    /// </summary>
    public interface IAreaNarrativeOrchestrator
    {
        /// <summary>
        /// Generates a complete area scenario with narrative content.
        /// Orchestrates the full pipeline: scenario -> context -> generation -> binding.
        /// </summary>
        /// <param name="context">The game context containing player state</param>
        /// <returns>Enriched scenario data with bound stories</returns>
        ScenarioData GenerateAreaScenario(GameContext context);

        /// <summary>
        /// Event fired when area generation completes successfully.
        /// Provides both the scenario data and narrative generation result.
        /// </summary>
        event Action<ScenarioData, GenerationResult> OnAreaGenerated;
    }
}
