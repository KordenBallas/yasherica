using System.Collections.Generic;
using System.Linq;
using LevelGeneration;
using Narrative.Data;
using Narrative.Data.Definitions;
using Narrative.Graph;

namespace Narrative.Selection
{
    /// <summary>
    /// Linear story selection strategy.
    /// Prioritizes main story progression with minimal side content.
    /// Best for players who want a focused narrative experience.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class LinearStoryStrategy : IStorySelectionStrategy
    {
        public string StrategyName => "Linear Progression";

        public string Description => "Prioritizes main story path with minimal side content. " +
                                     "Focuses on key progression and chapter advancement.";

        public IReadOnlyList<BaseStoryDefinition> SelectStories(
            IReadOnlyList<BaseStoryDefinition> availableStories,
            GameContext context,
            IStoryGraphProvider graph,
            int maxStories)
        {
            if (availableStories == null || availableStories.Count == 0)
                return System.Array.Empty<BaseStoryDefinition>();

            var selected = new List<(BaseStoryDefinition story, float score)>();

            foreach (var story in availableStories)
            {
                float score = CalculateScore(story, graph);
                selected.Add((story, score));
            }

            return selected
                .OrderByDescending(s => s.score)
                .Take(maxStories)
                .Select(s => s.story)
                .ToList();
        }

        private float CalculateScore(BaseStoryDefinition story, IStoryGraphProvider graph)
        {
            float score = 0f;

            // Highest priority: Main chapters
            if (story.StoryType == StoryType.Chapter)
            {
                score += 1000f;
            }

            // High priority: Key progression stories
            if (story.PlatformConfig.IsKeyProgression)
            {
                score += 500f;
            }

            // Medium priority: Sequence relationships (direct continuations)
            var node = graph.GetGraph().GetNode(story.StoryId);
            if (node != null)
            {
                int sequenceConnections = node.IncomingEdges.Count(e =>
                    e.Relationship.RelationshipType == StoryRelationshipType.Sequence &&
                    e.IsActive);

                score += sequenceConnections * 200f;
            }

            // Low priority: Side stories (only if they're required)
            if (story.StoryType == StoryType.SideStory)
            {
                // Check if it's a prerequisite for any main story
                bool isPrerequisite = IsPrerequisiteForMainStory(story, graph);
                if (isPrerequisite)
                {
                    score += 100f;
                }
                else
                {
                    score += 10f; // Very low priority
                }
            }

            // Add base priority as a tiebreaker
            score += story.GetBasePriority() * 0.1f;

            return score;
        }

        private bool IsPrerequisiteForMainStory(BaseStoryDefinition story, IStoryGraphProvider graph)
        {
            var node = graph.GetGraph().GetNode(story.StoryId);
            if (node == null)
                return false;

            // Check if any outgoing edges lead to chapters or key progression
            foreach (var edge in node.OutgoingEdges)
            {
                if (edge.ToNode.Definition.StoryType == StoryType.Chapter ||
                    edge.ToNode.Definition.PlatformConfig.IsKeyProgression)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
