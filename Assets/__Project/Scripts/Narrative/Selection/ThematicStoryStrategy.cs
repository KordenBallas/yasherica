using System.Collections.Generic;
using System.Linq;
using LevelGeneration;
using Narrative.Data;
using Narrative.Data.Definitions;
using Narrative.Graph;

namespace Narrative.Selection
{
    /// <summary>
    /// Thematic story selection strategy.
    /// Selects stories that match recent themes and attributes.
    /// Creates cohesive narrative arcs with strong thematic continuity.
    /// Best for players who want deep, focused storytelling experiences.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class ThematicStoryStrategy : IStorySelectionStrategy
    {
        public string StrategyName => "Thematic Continuity";

        public string Description => "Prioritizes stories matching recent themes and attributes. " +
                                     "Creates cohesive narrative arcs with strong thematic connections.";

        public IReadOnlyList<BaseStoryDefinition> SelectStories(
            IReadOnlyList<BaseStoryDefinition> availableStories,
            GameContext context,
            IStoryGraphProvider graph,
            int maxStories)
        {
            if (availableStories == null || availableStories.Count == 0)
                return System.Array.Empty<BaseStoryDefinition>();

            // Build thematic profile from completed stories
            var thematicProfile = BuildThematicProfile(graph);

            var selected = new List<(BaseStoryDefinition story, float score)>();

            foreach (var story in availableStories)
            {
                float score = CalculateThematicScore(story, graph, thematicProfile);
                selected.Add((story, score));
            }

            return selected
                .OrderByDescending(s => s.score)
                .Take(maxStories)
                .Select(s => s.story)
                .ToList();
        }

        private StoryAttributeCollection BuildThematicProfile(IStoryGraphProvider graph)
        {
            var profile = new StoryAttributeCollection();
            var completedNodes = graph.GetGraph().AllNodes
                .Where(n => n.State == StoryNodeState.Completed)
                .ToList();

            // Weight recent completions more heavily
            int recentCount = System.Math.Min(5, completedNodes.Count);
            var recentStories = completedNodes.Skip(completedNodes.Count - recentCount);

            foreach (var node in recentStories)
            {
                if (node.Definition.Attributes != null)
                {
                    profile.AddRange(node.Definition.Attributes);
                }
            }

            return profile;
        }

        private float CalculateThematicScore(
            BaseStoryDefinition story,
            IStoryGraphProvider graph,
            StoryAttributeCollection thematicProfile)
        {
            float score = story.GetBasePriority() * 0.5f; // Reduced base priority weight

            // Attribute matching is the primary scoring factor
            if (story.Attributes != null && story.Attributes.Count > 0 && thematicProfile.Attributes.Count > 0)
            {
                var storyAttributes = new StoryAttributeCollection(story.Attributes);
                float thematicScore = storyAttributes.GetMatchScore(thematicProfile);
                score += thematicScore * 500f; // Very high weight for thematic matches
            }

            // Boost for TagMatch relationship type
            var node = graph.GetGraph().GetNode(story.StoryId);
            if (node != null)
            {
                int tagMatchConnections = node.IncomingEdges.Count(e =>
                    e.IsActive &&
                    e.Relationship.RelationshipType == StoryRelationshipType.TagMatch);
                score += tagMatchConnections * 300f;

                // Boost for Callback relationships (narrative references)
                int callbackConnections = node.IncomingEdges.Count(e =>
                    e.IsActive &&
                    e.Relationship.RelationshipType == StoryRelationshipType.Callback);
                score += callbackConnections * 200f;
            }

            // Still prioritize key progression, but less than attribute matching
            if (story.PlatformConfig.IsKeyProgression)
            {
                score += 150f;
            }

            // Main chapters get moderate boost (not as high as thematic matches)
            if (story.StoryType == StoryType.Chapter)
            {
                score += 200f;
            }

            // Attribute-specific boosts
            if (story.Attributes != null)
            {
                foreach (var attr in story.Attributes)
                {
                    // Boost for high-weight attributes
                    if (attr.Weight > 0.7f)
                    {
                        score += 100f;
                    }

                    // Boost for thematic attributes (theme, emotion, tone)
                    if (attr.AttributeKey == StoryAttributeKeys.Theme ||
                        attr.AttributeKey == StoryAttributeKeys.Emotion ||
                        attr.AttributeKey == StoryAttributeKeys.Tone)
                    {
                        score += 150f;
                    }
                }
            }

            return score;
        }
    }
}
