using System.Collections.Generic;
using System.Linq;
using LevelGeneration;
using Narrative.Data;
using Narrative.Data.Definitions;
using Narrative.Graph;

namespace Narrative.Selection
{
    /// <summary>
    /// Exploration story selection strategy.
    /// Prioritizes side stories, world-building, and content variety.
    /// Discovers parallel storylines and maximizes unique content.
    /// Best for players who want to explore all available content.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class ExplorationStoryStrategy : IStorySelectionStrategy
    {
        public string StrategyName => "Content Exploration";

        public string Description => "Prioritizes side stories and world-building content. " +
                                     "Maximizes variety and discovers parallel storylines.";

        public IReadOnlyList<BaseStoryDefinition> SelectStories(
            IReadOnlyList<BaseStoryDefinition> availableStories,
            GameContext context,
            IStoryGraphProvider graph,
            int maxStories)
        {
            if (availableStories == null || availableStories.Count == 0)
                return System.Array.Empty<BaseStoryDefinition>();

            var selected = new List<(BaseStoryDefinition story, float score)>();

            // Track attribute diversity for variety scoring
            var seenAttributes = new HashSet<string>();

            foreach (var story in availableStories)
            {
                float score = CalculateExplorationScore(story, graph, seenAttributes);
                selected.Add((story, score));

                // Track attributes for diversity
                if (story.Attributes != null)
                {
                    foreach (var attr in story.Attributes)
                    {
                        seenAttributes.Add($"{attr.AttributeKey}:{attr.AttributeValue}");
                    }
                }
            }

            // Prioritize variety - try to get stories of different types and themes
            return SelectDiverseStories(selected, maxStories);
        }

        private float CalculateExplorationScore(
            BaseStoryDefinition story,
            IStoryGraphProvider graph,
            HashSet<string> seenAttributes)
        {
            float score = story.GetBasePriority() * 1.2f; // Slightly boosted base priority

            // INVERTED priorities - side content is more valuable than main story
            switch (story.StoryType)
            {
                case StoryType.SideStory:
                    score += 600f; // Highest priority
                    break;
                case StoryType.Dialogue:
                    score += 500f; // High priority for NPC interactions
                    break;
                case StoryType.Chapter:
                    score += 200f; // Lower priority for main story
                    break;
            }

            // Boost for Parallel relationships (multiple stories available at once)
            var node = graph.GetGraph().GetNode(story.StoryId);
            if (node != null)
            {
                int parallelConnections = node.IncomingEdges.Count(e =>
                    e.IsActive &&
                    e.Relationship.RelationshipType == StoryRelationshipType.Parallel);
                score += parallelConnections * 250f;

                // Boost for Branch relationships (alternative paths)
                int branchConnections = node.IncomingEdges.Count(e =>
                    e.IsActive &&
                    e.Relationship.RelationshipType == StoryRelationshipType.Branch);
                score += branchConnections * 200f;

                // Penalize Sequence relationships (too linear)
                int sequenceConnections = node.IncomingEdges.Count(e =>
                    e.IsActive &&
                    e.Relationship.RelationshipType == StoryRelationshipType.Sequence);
                score -= sequenceConnections * 50f;
            }

            // Diversity bonus - reward stories with unique attributes
            if (story.Attributes != null)
            {
                int uniqueAttributes = story.Attributes.Count(attr =>
                    !seenAttributes.Contains($"{attr.AttributeKey}:{attr.AttributeValue}"));

                score += uniqueAttributes * 150f;
            }

            // Boost for repeatable content (more content to explore)
            if (story.CanRepeat())
            {
                score += 100f;
            }

            // Small boost for key progression to maintain some structure
            if (story.PlatformConfig.IsKeyProgression)
            {
                score += 50f;
            }

            // Boost for location and genre diversity
            if (story.Attributes != null)
            {
                foreach (var attr in story.Attributes)
                {
                    if (attr.AttributeKey == StoryAttributeKeys.Location ||
                        attr.AttributeKey == StoryAttributeKeys.Genre)
                    {
                        score += 120f;
                    }
                }
            }

            return score;
        }

        private IReadOnlyList<BaseStoryDefinition> SelectDiverseStories(
            List<(BaseStoryDefinition story, float score)> scoredStories,
            int maxStories)
        {
            var result = new List<BaseStoryDefinition>();
            var usedTypes = new HashSet<StoryType>();
            var usedThemes = new HashSet<string>();

            // Sort by score
            var sorted = scoredStories.OrderByDescending(s => s.score).ToList();

            // First pass: Select diverse stories
            foreach (var (story, score) in sorted)
            {
                if (result.Count >= maxStories)
                    break;

                // Check if this adds diversity
                bool addsDiversity = !usedTypes.Contains(story.StoryType);

                // Check theme diversity
                if (story.Attributes != null)
                {
                    var themes = story.Attributes
                        .Where(a => a.AttributeKey == StoryAttributeKeys.Theme)
                        .Select(a => a.AttributeValue);

                    addsDiversity = addsDiversity || themes.Any(t => !usedThemes.Contains(t));
                }

                if (addsDiversity || result.Count < maxStories / 2)
                {
                    result.Add(story);
                    usedTypes.Add(story.StoryType);

                    if (story.Attributes != null)
                    {
                        foreach (var attr in story.Attributes)
                        {
                            if (attr.AttributeKey == StoryAttributeKeys.Theme)
                            {
                                usedThemes.Add(attr.AttributeValue);
                            }
                        }
                    }
                }
            }

            // Second pass: Fill remaining slots with highest-scored stories
            if (result.Count < maxStories)
            {
                var remaining = sorted
                    .Where(s => !result.Contains(s.story))
                    .Take(maxStories - result.Count)
                    .Select(s => s.story);

                result.AddRange(remaining);
            }

            return result;
        }
    }
}
