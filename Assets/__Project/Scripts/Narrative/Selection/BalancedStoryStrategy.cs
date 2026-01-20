using System.Collections.Generic;
using System.Linq;
using LevelGeneration;
using Narrative.Data;
using Narrative.Data.Definitions;
using Narrative.Graph;

namespace Narrative.Selection
{
    /// <summary>
    /// Balanced story selection strategy (RECOMMENDED DEFAULT).
    /// Mixes main story progression with connected side stories.
    /// Uses relationship weights to prioritize narratively connected content.
    /// Provides best overall player experience with variety and coherence.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class BalancedStoryStrategy : IStorySelectionStrategy
    {
        private const float MainStoryWeight = 1.0f;
        private const float SideStoryWeight = 0.6f;
        private const float ConnectionBonus = 0.4f;
        private const float AttributeMatchBonus = 0.3f;

        public string StrategyName => "Balanced Experience";

        public string Description => "Balances main story progression with connected side content. " +
                                     "Uses relationship weights and attributes for narrative coherence.";

        public IReadOnlyList<BaseStoryDefinition> SelectStories(
            IReadOnlyList<BaseStoryDefinition> availableStories,
            GameContext context,
            IStoryGraphProvider graph,
            int maxStories)
        {
            if (availableStories == null || availableStories.Count == 0)
                return System.Array.Empty<BaseStoryDefinition>();

            var selected = new List<(BaseStoryDefinition story, float score)>();

            // Build attribute collection from available stories for thematic matching
            var accumulatedAttributes = BuildAttributeCollection(availableStories);

            foreach (var story in availableStories)
            {
                float score = CalculateScore(story, graph, accumulatedAttributes);
                selected.Add((story, score));
            }

            // Sort by score and take mix of different types
            var sorted = selected.OrderByDescending(s => s.score).ToList();

            // Ensure we have at least one main story if available
            var result = new List<BaseStoryDefinition>();
            var mainStory = sorted.FirstOrDefault(s => s.story.StoryType == StoryType.Chapter ||
                                                        s.story.PlatformConfig.IsKeyProgression);

            if (mainStory.story != null)
            {
                result.Add(mainStory.story);
                sorted.Remove(mainStory);
            }

            // Fill remaining slots with best-scored stories
            result.AddRange(sorted.Take(maxStories - result.Count).Select(s => s.story));

            return result;
        }

        private float CalculateScore(
            BaseStoryDefinition story,
            IStoryGraphProvider graph,
            StoryAttributeCollection accumulatedAttributes)
        {
            float score = story.GetBasePriority(); // Base 0-100

            // Story type scoring
            switch (story.StoryType)
            {
                case StoryType.Chapter:
                    score += 500f * MainStoryWeight;
                    break;
                case StoryType.SideStory:
                    score += 300f * SideStoryWeight;
                    break;
                case StoryType.Dialogue:
                    score += 200f * SideStoryWeight;
                    break;
            }

            // Key progression boost
            if (story.PlatformConfig.IsKeyProgression)
            {
                score += 250f;
            }

            // Connection scoring - boost for stories connected to completed stories
            var node = graph.GetGraph().GetNode(story.StoryId);
            if (node != null)
            {
                // Incoming edges from completed stories
                int activeIncomingConnections = node.IncomingEdges.Count(e => e.IsActive);
                score += activeIncomingConnections * 150f * ConnectionBonus;

                // Weight-based boost from relationships
                float relationshipBoost = node.IncomingEdges
                    .Where(e => e.IsActive)
                    .Sum(e => e.Relationship.Weight * 100f);
                score += relationshipBoost * ConnectionBonus;

                // Boost for Sequence and Consequence relationships (strong narrative flow)
                int strongConnections = node.IncomingEdges.Count(e =>
                    e.IsActive &&
                    (e.Relationship.RelationshipType == StoryRelationshipType.Sequence ||
                     e.Relationship.RelationshipType == StoryRelationshipType.Consequence));
                score += strongConnections * 200f * ConnectionBonus;
            }

            // Attribute matching - boost for thematic consistency
            if (story.Attributes != null && story.Attributes.Count > 0 && accumulatedAttributes != null)
            {
                var storyAttributeCollection = new StoryAttributeCollection(story.Attributes);
                float attributeScore = storyAttributeCollection.GetMatchScore(accumulatedAttributes);
                score += attributeScore * 100f * AttributeMatchBonus;
            }

            return score;
        }

        private StoryAttributeCollection BuildAttributeCollection(IReadOnlyList<BaseStoryDefinition> stories)
        {
            var collection = new StoryAttributeCollection();

            foreach (var story in stories)
            {
                if (story.Attributes != null)
                {
                    collection.AddRange(story.Attributes);
                }
            }

            return collection;
        }
    }
}
