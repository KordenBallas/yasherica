using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative
{
    /// <summary>
    /// Interface for parsing story requirements from Ink content.
    /// </summary>
    public interface IStoryRequirementParser
    {
        /// <summary>
        /// Parses story node requirements from a chapter definition.
        /// </summary>
        IReadOnlyList<StoryNodeRequirement> ParseRequirements(StoryChapterDefinition chapter);

        /// <summary>
        /// Parses requirements from Ink tags.
        /// </summary>
        StoryNodeRequirement ParseFromTags(string knotName, IReadOnlyList<string> tags);

        /// <summary>
        /// Parses a single tag and applies it to the requirement.
        /// </summary>
        void ParseTag(string tag, StoryNodeRequirement requirement);
    }
}
