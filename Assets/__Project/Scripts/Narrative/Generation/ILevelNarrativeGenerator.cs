using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Generates narrative content (NPC assignments) for a level.
    /// </summary>
    public interface ILevelNarrativeGenerator
    {
        /// <summary>
        /// Generates NPC assignments for the given level configuration.
        /// </summary>
        LevelNarrative Generate(LevelNarrativeConfig config);
    }
}
