using System;
using System.Collections.Generic;
using Narrative.Data;
using Narrative.Data.Definitions;
using UnityEngine;

// IStoryStateProvider is defined in Narrative namespace
// The field is stored for future extensibility but not currently used

namespace Narrative.Generation
{
    /// <summary>
    /// Selects story templates based on world/player state.
    /// Evaluates eligibility and scores templates for selection.
    /// Applies different policies for Main Story vs Side Story types.
    /// Pure C# class - no Unity dependencies except logging.
    /// </summary>
    public class StoryTemplateSelector : IStoryTemplateSelector
    {
        private readonly IReadOnlyList<StoryTemplateDefinition> _allTemplates;
        private readonly IStoryStateProvider _storyStateProvider;
        private readonly IStoryPolicyProvider _policyProvider;

        /// <summary>
        /// Creates a new story template selector.
        /// </summary>
        public StoryTemplateSelector(
            IReadOnlyList<StoryTemplateDefinition> templates,
            IStoryPolicyProvider policyProvider,
            IStoryStateProvider storyStateProvider = null)
        {
            _allTemplates = templates ?? Array.Empty<StoryTemplateDefinition>();
            _policyProvider = policyProvider ?? new StoryPolicyProvider();
            _storyStateProvider = storyStateProvider;
        }

        public IReadOnlyList<StoryTemplateSelection> SelectTemplates(INarrativeContext context, int maxResults)
        {
            // Default to selecting any story type
            return SelectTemplates(context, maxResults, null);
        }

        /// <summary>
        /// Selects templates filtered by story type with policy-aware scoring.
        /// </summary>
        /// <param name="context">Current narrative context</param>
        /// <param name="maxResults">Maximum number of templates to return</param>
        /// <param name="storyType">Optional story type filter (null = all types)</param>
        /// <returns>Ranked list of eligible templates</returns>
        public IReadOnlyList<StoryTemplateSelection> SelectTemplates(
            INarrativeContext context,
            int maxResults,
            StoryType? storyType)
        {
            if (context == null || maxResults <= 0)
                return Array.Empty<StoryTemplateSelection>();

            var selections = new List<StoryTemplateSelection>();

            foreach (var template in _allTemplates)
            {
                // Filter by story type if specified
                if (storyType.HasValue && template.StoryType != storyType.Value)
                    continue;

                if (!IsTemplateEligible(template, context))
                    continue;

                float score = ScoreTemplate(template, context);
                var reasons = GetSelectionReasons(template, context);
                var requiredParams = new List<TemplateParameterSlot>(template.ParameterSlots);

                selections.Add(new StoryTemplateSelection(template, score, reasons, requiredParams));
            }

            // Sort by score descending
            selections.Sort((a, b) => b.Score.CompareTo(a.Score));

            // Return top N
            if (selections.Count > maxResults)
            {
                selections.RemoveRange(maxResults, selections.Count - maxResults);
            }

            string typeFilter = storyType.HasValue ? storyType.Value.ToString() : "any";
            Debug.Log($"[StoryTemplateSelector] Selected {selections.Count} {typeFilter} templates from {_allTemplates.Count} available");
            return selections;
        }

        public StoryTemplateSelection SelectBestTemplate(INarrativeContext context)
        {
            return SelectBestTemplate(context, null);
        }

        /// <summary>
        /// Gets the single best template for the current context, filtered by story type.
        /// </summary>
        /// <param name="context">Current narrative context</param>
        /// <param name="storyType">Optional story type filter (null = all types)</param>
        /// <returns>The best matching template or null if none available</returns>
        public StoryTemplateSelection SelectBestTemplate(INarrativeContext context, StoryType? storyType)
        {
            var selections = SelectTemplates(context, 1, storyType);
            return selections.Count > 0 ? selections[0] : null;
        }

        public bool IsTemplateEligible(StoryTemplateDefinition template, INarrativeContext context)
        {
            if (template == null || context == null)
                return false;

            // Check if template has content
            if (!template.HasInkContent)
                return false;

            // Check chapter requirements
            if (template.MinimumChapter > 0 && context.CurrentChapterNumber < template.MinimumChapter)
                return false;

            if (template.MaximumChapter > 0 && context.CurrentChapterNumber > template.MaximumChapter)
                return false;

            // Check theme requirements
            if (template.RequiresSpecificTheme && template.RequiredTheme != context.CurrentTheme)
                return false;

            // Check alignment requirements
            if (context.PlayerData != null)
            {
                int alignment = context.PlayerData.AlignmentScore;
                if (alignment < template.AlignmentRange.x || alignment > template.AlignmentRange.y)
                    return false;
            }

            // Check if recently played (for non-repeatable or cooldown)
            if (!template.IsRepeatable && context.WasRecentlyPlayed(template.StoryId))
                return false;

            // Check prerequisites
            if (template.Prerequisites.HasAnyPrerequisites && !ArePrerequisitesMet(template.Prerequisites, context))
                return false;

            // Check NPC availability
            if (template.RequiredNpcCount > 0)
            {
                int availableNpcs = context.AvailableNpcs?.Count ?? 0;
                if (availableNpcs < template.RequiredNpcCount)
                    return false;
            }

            return true;
        }

        public float ScoreTemplate(StoryTemplateDefinition template, INarrativeContext context)
        {
            if (template == null || context == null)
                return 0f;

            // Get policy for this story type
            var policy = _policyProvider.GetPolicy(template.StoryType);

            // Start with template's base priority plus policy bonus
            float score = template.GetBasePriority() + policy.BasePriorityBonus;

            // Bonus for matching theme (policy-defined)
            if (!template.RequiresSpecificTheme)
            {
                var theme = template.GetTheme();
                if (theme.HasValue && theme.Value == context.CurrentTheme)
                    score += policy.ThemeMatchBonus;
            }

            // Bonus for progression relevance (policy-defined)
            // Main stories get +20 for key progression, side stories get 0
            if (template.PlatformConfig.IsKeyProgression)
                score += policy.KeyProgressionBonus;

            // Bonus for variety (policy-defined)
            if (!context.WasRecentlyPlayed(template.StoryId))
                score += policy.VarietyBonus;

            // Bonus for attribute matching with player history
            if (context.WorldState?.AccumulatedAttributes != null)
            {
                score += CalculateAttributeBonus(template, context.WorldState.AccumulatedAttributes);
            }

            // Adjust for chapter position (prefer later templates later in chapter)
            float progressBonus = template.MinimumChapter > 0
                ? context.ChapterProgress * 5f
                : 0f;
            score += progressBonus;

            return score;
        }

        public IReadOnlyList<StoryTemplateDefinition> GetTemplatesByType(StoryType storyType)
        {
            var results = new List<StoryTemplateDefinition>();
            foreach (var template in _allTemplates)
            {
                if (template.StoryType == storyType)
                    results.Add(template);
            }
            return results;
        }

        private bool ArePrerequisitesMet(StoryPrerequisites prerequisites, INarrativeContext context)
        {
            var worldState = context.WorldState;
            if (worldState == null)
                return true;

            // Check completed stories
            foreach (var storyId in prerequisites.RequiredCompletedStories)
            {
                if (!worldState.IsNodeCompleted(storyId))
                    return false;
            }

            // Check completed quests
            foreach (var questId in prerequisites.RequiredCompletedQuests)
            {
                if (!worldState.IsQuestCompleted(questId))
                    return false;
            }

            // Check active quests
            foreach (var questId in prerequisites.RequiredActiveQuests)
            {
                if (!worldState.IsQuestActive(questId))
                    return false;
            }

            // Check encountered NPCs
            foreach (var npcId in prerequisites.RequiredEncounteredNpcs)
            {
                if (!worldState.HasEncounteredNpc(npcId))
                    return false;
            }

            return true;
        }

        private float CalculateAttributeBonus(StoryTemplateDefinition template, IReadOnlyList<Data.StoryAttribute> playerAttributes)
        {
            if (template.Attributes == null || template.Attributes.Count == 0)
                return 0f;

            float bonus = 0f;

            foreach (var templateAttr in template.Attributes)
            {
                foreach (var playerAttr in playerAttributes)
                {
                    if (templateAttr.Matches(playerAttr))
                    {
                        bonus += templateAttr.Weight * 10f;
                    }
                }
            }

            return bonus;
        }

        private IReadOnlyList<string> GetSelectionReasons(StoryTemplateDefinition template, INarrativeContext context)
        {
            var reasons = new List<string>();
            var policy = _policyProvider.GetPolicy(template.StoryType);

            // Story type identification
            if (template.StoryType == StoryType.Chapter)
                reasons.Add("Main story (narrative spine)");
            else if (template.StoryType == StoryType.SideStory)
                reasons.Add("Side story (optional content)");

            if (template.PlatformConfig.IsKeyProgression && policy.KeyProgressionBonus > 0)
                reasons.Add($"Key progression (+{policy.KeyProgressionBonus})");

            if (!context.WasRecentlyPlayed(template.StoryId))
                reasons.Add("Fresh content");

            if (!template.RequiresSpecificTheme)
            {
                var theme = template.GetTheme();
                if (theme.HasValue && theme.Value == context.CurrentTheme)
                    reasons.Add($"Matches {context.CurrentTheme} theme");
            }

            if (template.MinimumChapter > 0 && context.CurrentChapterNumber >= template.MinimumChapter)
                reasons.Add($"Unlocked at chapter {template.MinimumChapter}");

            // Policy-based NPC access
            if (policy.HasPrimaryNpcAccess)
                reasons.Add("Primary NPC pool access");

            return reasons;
        }

        /// <summary>
        /// Gets the policy provider used by this selector.
        /// </summary>
        public IStoryPolicyProvider PolicyProvider => _policyProvider;
    }
}
