using System.Collections.Generic;
using LevelGeneration;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Manages a pool of StoryDefinitions with filtering and cooldown tracking.
    /// </summary>
    public class StoryPool : IStoryPool
    {
        private readonly IReadOnlyList<StoryDefinition> _stories;
        private readonly Dictionary<string, int> _cooldowns = new();

        public int Count => _stories.Count;

        public StoryPool(IReadOnlyList<StoryDefinition> stories)
        {
            _stories = stories ?? System.Array.Empty<StoryDefinition>();
        }

        public IReadOnlyList<StoryDefinition> Filter(
            LevelTheme theme,
            int difficulty,
            IReadOnlyList<string> requiredTags,
            IReadOnlyList<string> excludedTags)
        {
            var results = new List<StoryDefinition>();

            for (int i = 0; i < _stories.Count; i++)
            {
                var story = _stories[i];

                if (IsOnCooldown(story.StoryId))
                    continue;

                if (!story.HasInkContent)
                    continue;

                if (!story.MatchesTheme(theme))
                    continue;

                if (!story.MatchesDifficulty(difficulty))
                    continue;

                if (!MatchesRequiredTags(story, requiredTags))
                    continue;

                if (HasExcludedTag(story, excludedTags))
                    continue;

                results.Add(story);
            }

            return results;
        }

        public void RecordUsage(string storyId)
        {
            if (string.IsNullOrEmpty(storyId))
                return;

            // Find the story to get its cooldown setting
            for (int i = 0; i < _stories.Count; i++)
            {
                if (_stories[i].StoryId == storyId)
                {
                    var story = _stories[i];
                    if (story.IsRepeatable && story.CooldownRuns > 0)
                    {
                        _cooldowns[storyId] = story.CooldownRuns;
                    }
                    else if (!story.IsRepeatable)
                    {
                        // Non-repeatable stories get permanent cooldown
                        _cooldowns[storyId] = int.MaxValue;
                    }
                    return;
                }
            }
        }

        public void TickCooldowns()
        {
            var expired = new List<string>();

            foreach (var kvp in _cooldowns)
            {
                if (kvp.Value == int.MaxValue)
                    continue;

                var remaining = kvp.Value - 1;
                if (remaining <= 0)
                    expired.Add(kvp.Key);
                else
                    _cooldowns[kvp.Key] = remaining;
            }

            for (int i = 0; i < expired.Count; i++)
            {
                _cooldowns.Remove(expired[i]);
            }
        }

        private bool IsOnCooldown(string storyId)
        {
            return _cooldowns.ContainsKey(storyId);
        }

        private static bool MatchesRequiredTags(StoryDefinition story, IReadOnlyList<string> requiredTags)
        {
            if (requiredTags == null || requiredTags.Count == 0)
                return true;

            for (int i = 0; i < requiredTags.Count; i++)
            {
                if (!story.HasTag(requiredTags[i]))
                    return false;
            }
            return true;
        }

        private static bool HasExcludedTag(StoryDefinition story, IReadOnlyList<string> excludedTags)
        {
            if (excludedTags == null || excludedTags.Count == 0)
                return false;

            for (int i = 0; i < excludedTags.Count; i++)
            {
                if (story.HasTag(excludedTags[i]))
                    return true;
            }
            return false;
        }
    }
}
