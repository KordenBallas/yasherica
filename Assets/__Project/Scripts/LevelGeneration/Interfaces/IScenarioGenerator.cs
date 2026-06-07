using Narrative.Generation;

namespace LevelGeneration
{
    /// <summary>
    /// Generates scenario data for level generation.
    /// Translates game context and narrative content into platform requirements.
    /// </summary>
    public interface IScenarioGenerator
    {
        /// <summary>
        /// Generates scenario data including platform requirements.
        /// </summary>
        /// <param name="context">Game context (difficulty, level, progress)</param>
        /// <param name="levelNarrative">Generated narrative assignments (NPCs + stories)</param>
        /// <returns>Scenario data with platform requirements</returns>
        ScenarioData GenerateScenario(GameContext context, LevelNarrative levelNarrative);
    }
}

