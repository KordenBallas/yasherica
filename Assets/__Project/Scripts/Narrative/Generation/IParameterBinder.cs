using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Interface for binding NPCs, locations, and rewards to story templates.
    /// Responsible for filling parameter slots with concrete values.
    /// </summary>
    public interface IParameterBinder
    {
        /// <summary>
        /// Binds all parameters for a story template selection.
        /// </summary>
        /// <param name="selection">The selected template with parameter requirements</param>
        /// <param name="context">Current narrative context</param>
        /// <param name="npcPool">Pool of available NPCs</param>
        /// <returns>Bound story ready for instantiation, or null if binding failed</returns>
        BoundStory BindParameters(
            StoryTemplateSelection selection,
            INarrativeContext context,
            INpcPool npcPool);

        /// <summary>
        /// Attempts to bind a specific parameter slot.
        /// </summary>
        /// <param name="slot">The parameter slot to fill</param>
        /// <param name="context">Current narrative context</param>
        /// <param name="npcPool">Pool of available NPCs</param>
        /// <returns>The bound value or null if binding failed</returns>
        BoundParameter BindParameter(
            TemplateParameterSlot slot,
            INarrativeContext context,
            INpcPool npcPool);

        /// <summary>
        /// Validates that all required parameters can be bound.
        /// </summary>
        /// <param name="selection">The template selection to validate</param>
        /// <param name="context">Current narrative context</param>
        /// <param name="npcPool">Pool of available NPCs</param>
        /// <returns>True if all parameters can be satisfied</returns>
        bool CanBindAllParameters(
            StoryTemplateSelection selection,
            INarrativeContext context,
            INpcPool npcPool);
    }

    /// <summary>
    /// Represents a fully bound story ready for instantiation.
    /// </summary>
    public class BoundStory
    {
        /// <summary>
        /// The original template.
        /// </summary>
        public StoryTemplateDefinition Template { get; }

        /// <summary>
        /// All bound parameters.
        /// </summary>
        public IReadOnlyDictionary<string, BoundParameter> BoundParameters { get; }

        /// <summary>
        /// The bound NPC instances.
        /// </summary>
        public IReadOnlyList<NpcInstance> BoundNpcs { get; }

        /// <summary>
        /// The bound location.
        /// </summary>
        public string BoundLocationId { get; }

        /// <summary>
        /// The bound rewards.
        /// </summary>
        public IReadOnlyList<RewardInstance> BoundRewards { get; }

        public BoundStory(
            StoryTemplateDefinition template,
            IReadOnlyDictionary<string, BoundParameter> boundParameters,
            IReadOnlyList<NpcInstance> boundNpcs,
            string boundLocationId,
            IReadOnlyList<RewardInstance> boundRewards)
        {
            Template = template;
            BoundParameters = boundParameters;
            BoundNpcs = boundNpcs;
            BoundLocationId = boundLocationId;
            BoundRewards = boundRewards;
        }
    }

    /// <summary>
    /// Represents a single bound parameter value.
    /// </summary>
    public class BoundParameter
    {
        /// <summary>
        /// The slot this parameter fills.
        /// </summary>
        public TemplateParameterSlot Slot { get; }

        /// <summary>
        /// The bound value (type depends on parameter type).
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Display-friendly string value for UI/dialogue.
        /// </summary>
        public string DisplayValue { get; }

        /// <summary>
        /// The value to inject into Ink variables.
        /// </summary>
        public string InkValue { get; }

        public BoundParameter(TemplateParameterSlot slot, object value, string displayValue, string inkValue)
        {
            Slot = slot;
            Value = value;
            DisplayValue = displayValue;
            InkValue = inkValue;
        }
    }
}
