using System.Collections.Generic;
using System.Linq;
using Narrative;
using Narrative.Data.Definitions;
using Narrative.Discovery;
using Narrative.Generation;
using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Scenario generator that integrates story requirements from Ink content.
    /// Queries IStoryStateProvider to create story-driven platforms mixed with procedural filler.
    /// Also integrates side stories via ISideStoryProvider.
    /// </summary>
    public class StoryAwareScenarioGenerator : IScenarioGenerator
    {
        private readonly IStoryStateProvider _storyStateProvider;
        private readonly ISideStoryProvider _sideStoryProvider;
        private readonly IStoryPolicyProvider _policyProvider;
        private readonly ScenarioGenerator _fallbackGenerator;

        private const int MinFillerPlatforms = 2;
        private const int MaxFillerPlatforms = 4;
        private const int MinSideStories = 0;
        private const int MaxSideStories = 2;

        public StoryAwareScenarioGenerator(
            IStoryStateProvider storyStateProvider,
            ISideStoryProvider sideStoryProvider = null,
            IStoryPolicyProvider policyProvider = null)
        {
            _storyStateProvider = storyStateProvider;
            _sideStoryProvider = sideStoryProvider;
            _policyProvider = policyProvider;
            _fallbackGenerator = new ScenarioGenerator(_policyProvider);
        }

        public ScenarioData GenerateScenario(GameContext context)
        {
            if (_storyStateProvider == null)
            {
                Debug.LogWarning("[StoryAwareScenarioGenerator] No story provider - using fallback generator");
                return _fallbackGenerator.GenerateScenario(context);
            }

            var storyRequirements = _storyStateProvider.GetCurrentRequirements();

            if (storyRequirements == null || storyRequirements.Count == 0)
            {
                Debug.Log("[StoryAwareScenarioGenerator] No story requirements - using fallback generator");
                return _fallbackGenerator.GenerateScenario(context);
            }

            return GenerateStoryScenario(context, storyRequirements);
        }

        private ScenarioData GenerateStoryScenario(GameContext context, IReadOnlyList<StoryNodeRequirement> storyRequirements)
        {
            var scenario = new ScenarioData
            {
                DifficultyLevel = CalculateDifficulty(context),
                Theme = SelectTheme(context),
                ChapterId = _storyStateProvider.CurrentChapterId
            };

            var requirements = new List<PlatformRequirement>();

            // Add entry platform
            requirements.Add(CreateEntryPlatform());

            // Sort story requirements by priority
            var sortedRequirements = storyRequirements
                .Where(r => !_storyStateProvider.CurrentState.IsNodeCompleted(r.NodeId))
                .OrderBy(r => r.Priority)
                .ToList();

            // Select side stories to include
            var selectedSideStories = SelectSideStories(context);
            int sideStoryIndex = 0;

            // Interleave story platforms with filler platforms and side stories
            int fillerCount = 0;
            int maxFillers = Random.Range(MinFillerPlatforms, MaxFillerPlatforms + 1);

            for (int i = 0; i < sortedRequirements.Count; i++)
            {
                var storyReq = sortedRequirements[i];

                // Add filler platform before key story platforms (but not the first one)
                if (i > 0 && storyReq.IsKeyNode && fillerCount < maxFillers)
                {
                    requirements.Add(CreateFillerPlatform(scenario.DifficultyLevel));
                    fillerCount++;
                }

                // Add story platform
                var platformReq = CreateStoryPlatform(storyReq);
                requirements.Add(platformReq);

                // Add filler after non-key platforms
                if (!storyReq.IsKeyNode && fillerCount < maxFillers && Random.value > 0.5f)
                {
                    requirements.Add(CreateFillerPlatform(scenario.DifficultyLevel));
                    fillerCount++;
                }

                // Insert side story after some story platforms
                if (sideStoryIndex < selectedSideStories.Count && Random.value > 0.6f)
                {
                    requirements.Add(CreateSideStoryPlatform(selectedSideStories[sideStoryIndex]));
                    sideStoryIndex++;
                }
            }

            // Add remaining side stories
            while (sideStoryIndex < selectedSideStories.Count)
            {
                int insertIndex = Random.Range(1, requirements.Count);
                requirements.Insert(insertIndex, CreateSideStoryPlatform(selectedSideStories[sideStoryIndex]));
                sideStoryIndex++;
            }

            // Ensure minimum filler platforms
            while (fillerCount < MinFillerPlatforms)
            {
                int insertIndex = Random.Range(1, requirements.Count);
                requirements.Insert(insertIndex, CreateFillerPlatform(scenario.DifficultyLevel));
                fillerCount++;
            }

            scenario.RequiredPlatforms = requirements;
            scenario.EstimatedPlatformCount = requirements.Count;

            Debug.Log($"[StoryAwareScenarioGenerator] Generated scenario: {requirements.Count} platforms " +
                      $"({sortedRequirements.Count} story, {selectedSideStories.Count} side stories, {fillerCount} filler)");

            return scenario;
        }

        private IReadOnlyList<SideStoryDefinition> SelectSideStories(GameContext context)
        {
            if (_sideStoryProvider == null)
                return new List<SideStoryDefinition>();

            var currentChapterNumber = ExtractChapterNumber(_storyStateProvider.CurrentChapterId);

            var selectionContext = new SideStorySelectionContext
            {
                StoryState = _storyStateProvider.CurrentState,
                CurrentChapterNumber = currentChapterNumber,
                MaxStoriesToSelect = Random.Range(MinSideStories, MaxSideStories + 1),
                ExcludeCompleted = true
            };

            return _sideStoryProvider.SelectWeightedRandomSideStories(selectionContext);
        }

        private int ExtractChapterNumber(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
                return 1;

            // Try to extract number from chapter ID (e.g., "chapter_3" -> 3)
            var parts = chapterId.Split('_');
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var number))
                    return number;
            }

            return 1;
        }

        private PlatformRequirement CreateSideStoryPlatform(SideStoryDefinition sideStory)
        {
            var contentTypes = new List<PlatformContentType>();
            var platformType = PlatformType.Simple;

            // Determine platform type and content based on side story
            switch (sideStory.PlatformType)
            {
                case StoryPlatformType.Combat:
                    platformType = PlatformType.Combat;
                    contentTypes.Add(PlatformContentType.Enemy);
                    break;

                case StoryPlatformType.Dialogue:
                    platformType = PlatformType.Simple;
                    if (sideStory.HasNpc)
                    {
                        contentTypes.Add(PlatformContentType.Npc);
                    }
                    else
                    {
                        contentTypes.Add(PlatformContentType.Dialogue);
                    }
                    break;

                case StoryPlatformType.Cutscene:
                    platformType = PlatformType.Simple;
                    contentTypes.Add(PlatformContentType.Cutscene);
                    break;

                default:
                    platformType = PlatformType.Simple;
                    if (sideStory.HasNpc)
                    {
                        contentTypes.Add(PlatformContentType.Npc);
                    }
                    break;
            }

            // If NPC can become enemy, set combat type
            if (sideStory.NpcCanBecomeEnemy)
            {
                platformType = PlatformType.Combat;
            }

            return new PlatformRequirement
            {
                Type = platformType,
                ContentTypes = contentTypes,
                StoryData = new StoryPlatformData
                {
                    SideStoryId = sideStory.StoryId,
                    NpcId = sideStory.NpcId,
                    DialogueKnot = sideStory.StartingKnot,
                    NpcCanBecomeEnemy = sideStory.NpcCanBecomeEnemy,
                    EnemyId = sideStory.EnemyId,
                    IsKeyNode = false,
                    Priority = sideStory.GetBasePriority()
                }
            };
        }

        private PlatformRequirement CreateEntryPlatform()
        {
            return new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
            };
        }

        private PlatformRequirement CreateStoryPlatform(StoryNodeRequirement storyReq)
        {
            var contentTypes = new List<PlatformContentType>();
            var platformType = PlatformType.Simple;

            // Determine platform type and content based on story requirement
            switch (storyReq.PlatformType)
            {
                case StoryPlatformType.Combat:
                    platformType = PlatformType.Combat;
                    contentTypes.Add(PlatformContentType.Enemy);
                    break;

                case StoryPlatformType.Dialogue:
                    platformType = PlatformType.Simple;
                    if (storyReq.HasNpc)
                    {
                        contentTypes.Add(PlatformContentType.Npc);
                    }
                    else
                    {
                        contentTypes.Add(PlatformContentType.Dialogue);
                    }
                    break;

                case StoryPlatformType.Cutscene:
                    platformType = PlatformType.Simple;
                    contentTypes.Add(PlatformContentType.Cutscene);
                    break;

                default:
                    platformType = PlatformType.Simple;
                    if (storyReq.HasNpc)
                    {
                        contentTypes.Add(PlatformContentType.Npc);
                    }
                    break;
            }

            // If NPC can become enemy, potentially add combat preparation
            if (storyReq.NpcCanBecomeEnemy && storyReq.ExpectedOutcome == DialogueOutcomeType.Combat)
            {
                platformType = PlatformType.Combat;
            }

            return new PlatformRequirement
            {
                Type = platformType,
                ContentTypes = contentTypes,
                StoryData = new StoryPlatformData(storyReq)
            };
        }

        private PlatformRequirement CreateFillerPlatform(int difficulty)
        {
            // Random filler platform type
            float roll = Random.value;

            if (roll < 0.4f)
            {
                // Empty traversal platform
                return new PlatformRequirement
                {
                    Type = PlatformType.Simple,
                    ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
                };
            }
            else if (roll < 0.75f)
            {
                // Combat encounter
                return new PlatformRequirement
                {
                    Type = PlatformType.Combat,
                    ContentTypes = new List<PlatformContentType>
                    {
                        PlatformContentType.Enemy,
                        Random.value > 0.5f ? PlatformContentType.Loot : PlatformContentType.None
                    }
                };
            }
            else
            {
                // Loot only
                return new PlatformRequirement
                {
                    Type = PlatformType.Simple,
                    ContentTypes = new List<PlatformContentType> { PlatformContentType.Loot }
                };
            }
        }

        private int CalculateDifficulty(GameContext context)
        {
            // Base difficulty from character level
            int baseDifficulty = Mathf.Max(1, context.CharacterLevel / 5);

            // Story progression can increase difficulty
            int storyBonus = context.StoryState / 3;

            return Mathf.Min(10, baseDifficulty + storyBonus);
        }

        private LevelTheme SelectTheme(GameContext context)
        {
            // Try to get theme from current chapter
            var state = _storyStateProvider?.CurrentState;
            if (state != null && !string.IsNullOrEmpty(state.CurrentChapterId))
            {
                // Theme could be determined by chapter, but for now use story state
                var themes = System.Enum.GetValues(typeof(LevelTheme)).Cast<LevelTheme>().ToArray();
                return themes[context.StoryState % themes.Length];
            }

            // Fallback to context-based selection
            var allThemes = System.Enum.GetValues(typeof(LevelTheme)).Cast<LevelTheme>().ToArray();
            return allThemes[context.StoryState % allThemes.Length];
        }
    }
}
