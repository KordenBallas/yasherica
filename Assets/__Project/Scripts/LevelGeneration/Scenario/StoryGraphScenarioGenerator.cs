using System;
using System.Collections.Generic;
using System.Linq;
using Narrative;
using Narrative.Data;
using Narrative.Data.Definitions;
using Narrative.Graph;
using Narrative.Selection;

namespace LevelGeneration
{
    /// <summary>
    /// Enhanced scenario generator using story graph and selection strategies.
    /// Replaces simple story injection with intelligent, graph-based narrative generation.
    /// Pure C# class - no Unity dependencies (follows MVP pattern).
    /// </summary>
    public class StoryGraphScenarioGenerator : IScenarioGenerator
    {
        private readonly IStoryGraphProvider _storyGraph;
        private readonly IStoryStateProvider _storyState;
        private readonly IStorySelectionStrategy _selectionStrategy;

        private const int MinPlatforms = 5;
        private const int MaxPlatforms = 15;
        private const int BaseFillerPlatforms = 3;

        public StoryGraphScenarioGenerator(
            IStoryGraphProvider storyGraph,
            IStoryStateProvider storyState,
            IStorySelectionStrategy selectionStrategy)
        {
            _storyGraph = storyGraph ?? throw new ArgumentNullException(nameof(storyGraph));
            _storyState = storyState ?? throw new ArgumentNullException(nameof(storyState));
            _selectionStrategy = selectionStrategy ?? throw new ArgumentNullException(nameof(selectionStrategy));
        }

        public ScenarioData GenerateScenario(GameContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // 1. Get available stories from graph
            var availableStories = _storyGraph.GetAvailableStories(_storyState.CurrentState);

            // 2. Apply selection strategy
            int maxStories = CalculateMaxStories(context);
            var selectedStories = _selectionStrategy.SelectStories(
                availableStories,
                context,
                _storyGraph,
                maxStories
            );

            // 3. Prioritize connected stories for narrative flow
            var prioritizedStories = PrioritizeConnectedStories(
                selectedStories,
                _storyState.CurrentState.CompletedNodeIds
            );

            // 4. Build scenario from selected stories
            return BuildScenarioFromStories(prioritizedStories, context);
        }

        private int CalculateMaxStories(GameContext context)
        {
            // Adjust story count based on player progress
            int baseCount = 3;
            int progressBonus = context.Progress / 20; // +1 story every 20 progress
            int levelBonus = context.CharacterLevel / 5; // +1 story every 5 levels

            return Math.Min(baseCount + progressBonus + levelBonus, 6);
        }

        private IReadOnlyList<BaseStoryDefinition> PrioritizeConnectedStories(
            IReadOnlyList<BaseStoryDefinition> stories,
            IReadOnlyList<string> completedStories)
        {
            if (stories == null || stories.Count == 0)
                return Array.Empty<BaseStoryDefinition>();

            var scored = new List<(BaseStoryDefinition story, float connectionScore)>();

            foreach (var story in stories)
            {
                float score = GetConnectionScore(story, completedStories);
                scored.Add((story, score));
            }

            return scored
                .OrderByDescending(s => s.connectionScore)
                .ThenByDescending(s => s.story.PlatformConfig.IsKeyProgression)
                .Select(s => s.story)
                .ToList();
        }

        private float GetConnectionScore(BaseStoryDefinition story, IReadOnlyList<string> completedStories)
        {
            float score = 0f;

            var node = _storyGraph.GetGraph().GetNode(story.StoryId);
            if (node == null)
                return score;

            // Score based on incoming edges from completed stories
            foreach (var edge in node.IncomingEdges)
            {
                if (!edge.IsActive)
                    continue;

                if (completedStories.Contains(edge.FromNode.StoryId))
                {
                    // Weight by relationship type
                    switch (edge.Relationship.RelationshipType)
                    {
                        case StoryRelationshipType.Sequence:
                            score += 100f; // Highest priority - direct continuation
                            break;
                        case StoryRelationshipType.Consequence:
                            score += 80f; // High priority - story outcome
                            break;
                        case StoryRelationshipType.Callback:
                            score += 60f; // Medium-high - narrative reference
                            break;
                        case StoryRelationshipType.Branch:
                            score += 50f; // Medium - branching choice
                            break;
                        case StoryRelationshipType.TagMatch:
                            score += 40f; // Medium-low - thematic connection
                            break;
                        case StoryRelationshipType.Parallel:
                            score += 20f; // Low - available simultaneously
                            break;
                    }

                    // Add relationship weight
                    score += edge.Relationship.Weight * 50f;

                    // Boost for key progression relationships
                    if (edge.Relationship.IsKeyProgression)
                    {
                        score += 100f;
                    }
                }
            }

            return score;
        }

        private ScenarioData BuildScenarioFromStories(
            IReadOnlyList<BaseStoryDefinition> stories,
            GameContext context)
        {
            var scenario = new ScenarioData
            {
                DifficultyLevel = CalculateDifficulty(context, stories),
                Theme = SelectTheme(context, stories),
                ChapterId = _storyState.CurrentChapterId
            };

            var requirements = new List<PlatformRequirement>();

            // 1. Create entry platform (always first)
            requirements.Add(CreateEntryPlatform(scenario.Theme));

            // 2. Add story platforms with intelligent pacing
            InterleavePlatforms(requirements, stories, scenario.Theme);

            // 3. Add exit platform (always last)
            requirements.Add(CreateExitPlatform(scenario.Theme));

            scenario.RequiredPlatforms = requirements;
            scenario.EstimatedPlatformCount = requirements.Count;

            return scenario;
        }

        private int CalculateDifficulty(GameContext context, IReadOnlyList<BaseStoryDefinition> stories)
        {
            // Base difficulty on character level
            int difficulty = context.CharacterLevel / 2;

            // Adjust based on story types
            int combatStories = stories.Count(s => s.GetPlatformType() == StoryPlatformType.Combat);
            difficulty += combatStories;

            // Chapter stories are slightly harder
            int chapters = stories.Count(s => s.StoryType == StoryType.Chapter);
            difficulty += chapters;

            return Math.Max(1, Math.Min(difficulty, 10));
        }

        private LevelTheme SelectTheme(GameContext context, IReadOnlyList<BaseStoryDefinition> stories)
        {
            // Priority 1: Use theme from story overrides
            foreach (var story in stories)
            {
                var theme = story.GetTheme();
                if (theme.HasValue)
                {
                    return theme.Value;
                }
            }

            // Priority 2: Use theme from story attributes
            var themeAttributes = new Dictionary<string, int>();
            foreach (var story in stories)
            {
                if (story.Attributes != null)
                {
                    foreach (var attr in story.Attributes)
                    {
                        if (attr.AttributeKey == StoryAttributeKeys.Location)
                        {
                            if (!themeAttributes.ContainsKey(attr.AttributeValue))
                                themeAttributes[attr.AttributeValue] = 0;
                            themeAttributes[attr.AttributeValue]++;
                        }
                    }
                }
            }

            if (themeAttributes.Count > 0)
            {
                var mostCommonLocation = themeAttributes.OrderByDescending(kvp => kvp.Value).First().Key;
                return MapLocationToTheme(mostCommonLocation);
            }

            // Priority 3: Random based on progress
            var themes = Enum.GetValues(typeof(LevelTheme)).Cast<LevelTheme>().ToList();
            return themes[context.Progress % themes.Count];
        }

        private LevelTheme MapLocationToTheme(string location)
        {
            // Simple mapping - could be more sophisticated
            if (location.Contains("forest") || location.Contains("wood"))
                return LevelTheme.Forest;
            if (location.Contains("desert") || location.Contains("sand"))
                return LevelTheme.Desert;
            if (location.Contains("mountain") || location.Contains("peak"))
                return LevelTheme.Mountain;
            if (location.Contains("cave") || location.Contains("underground"))
                return LevelTheme.Cave;

            return LevelTheme.Forest;
        }

        private void InterleavePlatforms(
            List<PlatformRequirement> requirements,
            IReadOnlyList<BaseStoryDefinition> stories,
            LevelTheme theme)
        {
            int fillerCount = BaseFillerPlatforms;
            int totalStoryPlatforms = stories.Count;
            int maxPlatforms = Math.Min(totalStoryPlatforms + fillerCount, MaxPlatforms - 2); // -2 for entry/exit

            // Create story platforms
            var storyPlatforms = new List<PlatformRequirement>();
            foreach (var story in stories)
            {
                var platform = CreateStoryPlatform(story);
                storyPlatforms.Add(platform);
            }

            // Interleave story platforms with filler
            int platformIndex = 0;
            int fillerInserted = 0;

            for (int i = 0; i < storyPlatforms.Count && requirements.Count < maxPlatforms; i++)
            {
                // Add story platform
                requirements.Add(storyPlatforms[i]);
                platformIndex++;

                // Add filler every 2-3 story platforms
                if (platformIndex % 2 == 0 && fillerInserted < fillerCount && requirements.Count < maxPlatforms - 1)
                {
                    requirements.Add(CreateFillerPlatform(theme));
                    fillerInserted++;
                }
            }

            // Fill remaining space with filler if needed
            while (requirements.Count < MinPlatforms - 1 && requirements.Count < maxPlatforms)
            {
                requirements.Add(CreateFillerPlatform(theme));
            }
        }

        private PlatformRequirement CreateStoryPlatform(BaseStoryDefinition story)
        {
            var platformType = MapStoryPlatformTypeToPlatformType(story.GetPlatformType());

            var requirement = new PlatformRequirement
            {
                Type = platformType,
                ContentTypes = GetContentTypesForStory(story),
                StoryData = new StoryPlatformData
                {
                    SideStoryId = story.StoryType != StoryType.Chapter ? story.StoryId : null,
                    IsKeyNode = story.PlatformConfig.IsKeyProgression || story.StoryType == StoryType.Chapter,
                    Priority = story.GetBasePriority(),
                    DialogueKnot = story.StartingKnot,
                    // NPC data will be filled by NPC association system later
                }
            };

            return requirement;
        }

        private PlatformType MapStoryPlatformTypeToPlatformType(StoryPlatformType storyType)
        {
            switch (storyType)
            {
                case StoryPlatformType.Combat:
                    return PlatformType.Combat;
                case StoryPlatformType.Dialogue:
                case StoryPlatformType.Cutscene:
                case StoryPlatformType.Simple:
                default:
                    return PlatformType.Simple;
            }
        }

        private List<PlatformContentType> GetContentTypesForStory(BaseStoryDefinition story)
        {
            var contentTypes = new List<PlatformContentType>();

            switch (story.GetPlatformType())
            {
                case StoryPlatformType.Combat:
                    contentTypes.Add(PlatformContentType.Enemy);
                    break;

                case StoryPlatformType.Dialogue:
                    contentTypes.Add(PlatformContentType.Npc);
                    break;

                case StoryPlatformType.Cutscene:
                    contentTypes.Add(PlatformContentType.Npc);
                    break;

                case StoryPlatformType.Simple:
                    // Could have various content
                    break;
            }

            return contentTypes;
        }

        private PlatformRequirement CreateEntryPlatform()
        {
            return new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
            };
        }

        private PlatformRequirement CreateEntryPlatform(LevelTheme theme)
        {
            return CreateEntryPlatform();
        }

        private PlatformRequirement CreateExitPlatform(LevelTheme theme)
        {
            return new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
            };
        }

        private PlatformRequirement CreateFillerPlatform(LevelTheme theme)
        {
            // Random filler platform
            var rand = new Random();
            bool isCombat = rand.Next(0, 2) == 0;

            return new PlatformRequirement
            {
                Type = isCombat ? PlatformType.Combat : PlatformType.Simple,
                ContentTypes = isCombat
                    ? new List<PlatformContentType> { PlatformContentType.Enemy }
                    : new List<PlatformContentType> { PlatformContentType.Loot }
            };
        }
    }
}
