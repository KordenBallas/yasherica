using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Interface for selecting story templates based on world/player state.
    /// Determines which templates are eligible and ranks them by appropriateness.
    /// Applies different scoring policies for Main Story vs Side Story types.
    /// </summary>
    public interface IStoryTemplateSelector
    {
        /// <summary>
        /// Selects the most appropriate templates for the current context.
        /// </summary>
        /// <param name="context">Current narrative context</param>
        /// <param name="maxResults">Maximum number of templates to return</param>
        /// <returns>Ranked list of eligible templates</returns>
        IReadOnlyList<StoryTemplateSelection> SelectTemplates(INarrativeContext context, int maxResults);

        /// <summary>
        /// Selects the most appropriate templates filtered by story type.
        /// </summary>
        /// <param name="context">Current narrative context</param>
        /// <param name="maxResults">Maximum number of templates to return</param>
        /// <param name="storyType">Optional story type filter (null = all types)</param>
        /// <returns>Ranked list of eligible templates of the specified type</returns>
        IReadOnlyList<StoryTemplateSelection> SelectTemplates(INarrativeContext context, int maxResults, StoryType? storyType);

        /// <summary>
        /// Gets the single best template for the current context.
        /// </summary>
        /// <param name="context">Current narrative context</param>
        /// <returns>The best matching template or null if none available</returns>
        StoryTemplateSelection SelectBestTemplate(INarrativeContext context);

        /// <summary>
        /// Gets the single best template filtered by story type.
        /// </summary>
        /// <param name="context">Current narrative context</param>
        /// <param name="storyType">Optional story type filter (null = all types)</param>
        /// <returns>The best matching template or null if none available</returns>
        StoryTemplateSelection SelectBestTemplate(INarrativeContext context, StoryType? storyType);

        /// <summary>
        /// Checks if a specific template is eligible in the current context.
        /// </summary>
        /// <param name="template">The template to check</param>
        /// <param name="context">Current narrative context</param>
        /// <returns>True if the template can be used</returns>
        bool IsTemplateEligible(StoryTemplateDefinition template, INarrativeContext context);

        /// <summary>
        /// Calculates a score for a template in the current context.
        /// Scoring applies policy-based bonuses for story type.
        /// </summary>
        /// <param name="template">The template to score</param>
        /// <param name="context">Current narrative context</param>
        /// <returns>Score value (higher is better)</returns>
        float ScoreTemplate(StoryTemplateDefinition template, INarrativeContext context);

        /// <summary>
        /// Gets templates by type (main story, side story, etc.).
        /// </summary>
        /// <param name="storyType">Type of stories to retrieve</param>
        /// <returns>All templates of the specified type</returns>
        IReadOnlyList<StoryTemplateDefinition> GetTemplatesByType(StoryType storyType);
    }

    /// <summary>
    /// Represents a selected story template with scoring information.
    /// </summary>
    public class StoryTemplateSelection
    {
        /// <summary>
        /// The selected template.
        /// </summary>
        public StoryTemplateDefinition Template { get; }

        /// <summary>
        /// Selection score (higher = more appropriate).
        /// </summary>
        public float Score { get; }

        /// <summary>
        /// Reasons why this template was selected/ranked.
        /// </summary>
        public IReadOnlyList<string> SelectionReasons { get; }

        /// <summary>
        /// Parameters that need to be bound for this template.
        /// </summary>
        public IReadOnlyList<TemplateParameterSlot> RequiredParameters { get; }

        public StoryTemplateSelection(
            StoryTemplateDefinition template,
            float score,
            IReadOnlyList<string> selectionReasons,
            IReadOnlyList<TemplateParameterSlot> requiredParameters)
        {
            Template = template;
            Score = score;
            SelectionReasons = selectionReasons ?? System.Array.Empty<string>();
            RequiredParameters = requiredParameters ?? System.Array.Empty<TemplateParameterSlot>();
        }
    }
}
