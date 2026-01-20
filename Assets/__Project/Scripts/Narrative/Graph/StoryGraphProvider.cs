using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Data;
using Narrative.Data.Definitions;

namespace Narrative.Graph
{
    /// <summary>
    /// Implementation of story graph provider service.
    /// Manages the story graph and provides query capabilities.
    /// Pure C# class - no Unity dependencies (follows MVP pattern).
    /// </summary>
    public class StoryGraphProvider : IStoryGraphProvider
    {
        private readonly StoryGraph _graph;
        private readonly IReadOnlyList<BaseStoryDefinition> _allStories;
        private StoryState _currentState;

        public StoryGraphProvider(IReadOnlyList<BaseStoryDefinition> storyDefinitions)
        {
            _allStories = storyDefinitions ?? throw new ArgumentNullException(nameof(storyDefinitions));
            _graph = new StoryGraph();
            _graph.BuildGraph(_allStories);
        }

        public IReadOnlyList<BaseStoryDefinition> GetAvailableStories(StoryState currentState)
        {
            if (currentState == null)
                return Array.Empty<BaseStoryDefinition>();

            _currentState = currentState;
            _graph.UpdateNodeStates(currentState);

            var availableNodes = _graph.GetAvailableNodes();
            return availableNodes.Select(n => n.Definition).ToList();
        }

        public IReadOnlyList<BaseStoryDefinition> GetConnectedStories(
            BaseStoryDefinition fromStory,
            StoryRelationshipType? filterType = null)
        {
            if (fromStory == null || string.IsNullOrEmpty(fromStory.StoryId))
                return Array.Empty<BaseStoryDefinition>();

            var node = _graph.GetNode(fromStory.StoryId);
            if (node == null)
                return Array.Empty<BaseStoryDefinition>();

            var edges = node.OutgoingEdges;
            if (filterType.HasValue)
            {
                edges = edges.Where(e => e.Relationship.RelationshipType == filterType.Value).ToList();
            }

            return edges
                .Where(e => e.IsActive)
                .Select(e => e.ToNode.Definition)
                .ToList();
        }

        public IReadOnlyList<BaseStoryDefinition> GetStoriesByAttributes(
            IReadOnlyList<StoryAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0)
                return Array.Empty<BaseStoryDefinition>();

            var queryCollection = new StoryAttributeCollection(attributes);
            var results = new List<(BaseStoryDefinition story, float score)>();

            foreach (var story in _allStories)
            {
                if (story.Attributes == null || story.Attributes.Count == 0)
                    continue;

                var storyCollection = new StoryAttributeCollection(story.Attributes);
                float score = storyCollection.GetMatchScore(queryCollection);

                if (score > 0)
                {
                    results.Add((story, score));
                }
            }

            return results
                .OrderByDescending(r => r.score)
                .Select(r => r.story)
                .ToList();
        }

        public bool IsStoryAvailable(BaseStoryDefinition story, StoryState currentState)
        {
            if (story == null || currentState == null)
                return false;

            var node = _graph.GetNode(story.StoryId);
            if (node == null)
                return false;

            _graph.UpdateNodeStates(currentState);
            return node.State == StoryNodeState.Available;
        }

        public IReadOnlyList<BaseStoryDefinition> GetCriticalPath()
        {
            var criticalNodes = _graph.GetCriticalPath();
            return criticalNodes.Select(n => n.Definition).ToList();
        }

        public BaseStoryDefinition GetNextStory(StoryState currentState, StorySelectionContext context)
        {
            if (currentState == null || context == null)
                return null;

            _currentState = currentState;
            _graph.UpdateNodeStates(currentState);

            var availableStories = GetAvailableStories(currentState);
            if (availableStories.Count == 0)
                return null;

            // Filter by preferred story type if specified
            if (context.PreferredStoryType.HasValue)
            {
                var filtered = availableStories
                    .Where(s => s.StoryType == context.PreferredStoryType.Value)
                    .ToList();

                if (filtered.Count > 0)
                    availableStories = filtered;
            }

            // Score stories based on context
            var scoredStories = ScoreStories(availableStories, context);

            // Return highest scored story
            return scoredStories.FirstOrDefault().story;
        }

        private List<(BaseStoryDefinition story, float score)> ScoreStories(
            IReadOnlyList<BaseStoryDefinition> stories,
            StorySelectionContext context)
        {
            var results = new List<(BaseStoryDefinition story, float score)>();

            foreach (var story in stories)
            {
                float score = CalculateStoryScore(story, context);
                results.Add((story, score));
            }

            return results.OrderByDescending(r => r.score).ToList();
        }

        private float CalculateStoryScore(BaseStoryDefinition story, StorySelectionContext context)
        {
            float score = story.GetBasePriority(); // Base priority (0-100)

            // Boost for connected stories
            if (context.PrioritizeConnectedStories && context.RecentlyCompletedStories.Count > 0)
            {
                foreach (var completedStoryId in context.RecentlyCompletedStories)
                {
                    if (IsConnectedTo(completedStoryId, story.StoryId))
                    {
                        score += 50f; // Significant boost for connected stories
                        break;
                    }
                }
            }

            // Boost for attribute matches
            if (context.AccumulatedAttributes != null && story.Attributes.Count > 0)
            {
                var storyCollection = new StoryAttributeCollection(story.Attributes);
                float attributeScore = storyCollection.GetMatchScore(context.AccumulatedAttributes);
                score += attributeScore * 20f; // Boost based on attribute matching
            }

            // Boost for key progression
            if (story.PlatformConfig.IsKeyProgression)
            {
                score += 30f;
            }

            // Boost for main story chapters
            if (story.StoryType == StoryType.Chapter)
            {
                score += 40f;
            }

            return score;
        }

        private bool IsConnectedTo(string fromStoryId, string toStoryId)
        {
            var fromNode = _graph.GetNode(fromStoryId);
            if (fromNode == null)
                return false;

            return fromNode.OutgoingEdges.Any(e => e.IsActive && e.ToNode.StoryId == toStoryId);
        }

        public void CompleteStory(string storyId)
        {
            if (_currentState == null || string.IsNullOrEmpty(storyId))
                return;

            // Mark story as completed in state
            _currentState.CompleteNode(storyId);

            // Update graph to activate new edges
            _graph.UpdateNodeStates(_currentState);
        }

        public void RefreshGraph()
        {
            _graph.BuildGraph(_allStories);
            if (_currentState != null)
            {
                _graph.UpdateNodeStates(_currentState);
            }
        }

        public GraphStatistics GetStatistics()
        {
            return _graph.GetStatistics();
        }

        public StoryGraph GetGraph()
        {
            return _graph;
        }
    }
}
