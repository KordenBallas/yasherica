using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Discovery
{
    /// <summary>
    /// Provides filtered and weighted side story selection.
    /// Handles cooldowns for repeatable stories.
    /// </summary>
    public class SideStoryProvider : ISideStoryProvider
    {
        private readonly ISideStoryDiscoveryService _discoveryService;
        private readonly SideStoryPrerequisiteEvaluator _prerequisiteEvaluator;

        private readonly Dictionary<string, int> _lastPlayedAtPlatform = new();
        private int _platformCounter;

        public SideStoryProvider(
            ISideStoryDiscoveryService discoveryService,
            SideStoryPrerequisiteEvaluator prerequisiteEvaluator)
        {
            _discoveryService = discoveryService;
            _prerequisiteEvaluator = prerequisiteEvaluator;
        }

        public IReadOnlyList<SideStoryDefinition> GetAvailableSideStories(SideStorySelectionContext context)
        {
            if (context?.StoryState == null)
                return new List<SideStoryDefinition>();

            var candidates = GetCandidates(context);

            return candidates
                .Where(story => MeetsAllCriteria(story, context))
                .ToList();
        }

        public IReadOnlyList<SideStoryDefinition> SelectWeightedRandomSideStories(SideStorySelectionContext context)
        {
            var available = GetAvailableSideStories(context);

            if (available.Count == 0)
                return new List<SideStoryDefinition>();

            var maxToSelect = Math.Max(1, context.MaxStoriesToSelect);
            var selected = new List<SideStoryDefinition>();
            var remaining = available.ToList();

            while (selected.Count < maxToSelect && remaining.Count > 0)
            {
                var pick = SelectWeightedRandom(remaining);
                if (pick != null)
                {
                    selected.Add(pick);
                    remaining.Remove(pick);
                }
                else
                {
                    break;
                }
            }

            return selected;
        }

        public SideStoryDefinition GetSideStoryById(string storyId)
        {
            return _discoveryService.GetById(storyId);
        }

        public bool IsOnCooldown(string storyId)
        {
            if (!_lastPlayedAtPlatform.TryGetValue(storyId, out var lastPlayed))
                return false;

            var story = _discoveryService.GetById(storyId);
            if (story == null || !story.IsRepeatable)
                return false;

            return (_platformCounter - lastPlayed) < story.CooldownPlatforms;
        }

        public void RecordSideStoryPlayed(string storyId)
        {
            _lastPlayedAtPlatform[storyId] = _platformCounter;
        }

        public void IncrementPlatformCounter()
        {
            _platformCounter++;
        }

        private IEnumerable<SideStoryDefinition> GetCandidates(SideStorySelectionContext context)
        {
            // Start with all stories
            IEnumerable<SideStoryDefinition> candidates = _discoveryService.AllSideStories;

            // Filter by NPC if specified
            if (!string.IsNullOrEmpty(context.FilterNpcId))
            {
                candidates = _discoveryService.GetByNpc(context.FilterNpcId);
            }
            // Filter by tags if specified
            else if (context.FilterTags != null && context.FilterTags.Count > 0)
            {
                candidates = _discoveryService.GetByAttrs(context.FilterTags);
            }

            return candidates;
        }

        private bool MeetsAllCriteria(SideStoryDefinition story, SideStorySelectionContext context)
        {
            // Check prerequisites
            if (!_prerequisiteEvaluator.ArePrerequisitesMet(
                story,
                context.StoryState,
                context.CurrentChapterNumber))
            {
                return false;
            }

            // Check if already completed (for non-repeatable)
            if (context.ExcludeCompleted && !story.IsRepeatable)
            {
                var nodeId = $"sidestory_{story.StoryId}";
                if (context.StoryState.IsNodeCompleted(nodeId))
                    return false;
            }

            // Check cooldown for repeatable stories
            if (story.IsRepeatable && IsOnCooldown(story.StoryId))
                return false;

            return true;
        }

        private SideStoryDefinition SelectWeightedRandom(IReadOnlyList<SideStoryDefinition> stories)
        {
            if (stories == null || stories.Count == 0)
                return null;

            // Calculate total weight
            int totalWeight = 0;
            foreach (var story in stories)
            {
                totalWeight += Math.Max(1, story.GetBasePriority());
            }

            // Select random weighted
            var roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var story in stories)
            {
                cumulative += Math.Max(1, story.GetBasePriority());
                if (roll < cumulative)
                    return story;
            }

            // Fallback to first
            return stories[0];
        }
    }
}
