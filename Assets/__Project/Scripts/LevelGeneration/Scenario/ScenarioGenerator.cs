using System.Collections.Generic;
using System.Linq;
using Loot.Core;
using Narrative.Generation;
using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Basic scenario generator that creates a simple platform layout.
    /// Used as fallback when no specialized generator is configured.
    /// </summary>
    public class ScenarioGenerator : IScenarioGenerator
    {
        private readonly ILootRollService _lootRollService;

        public ScenarioGenerator(ILootRollService lootRollService)
        {
            _lootRollService = lootRollService;
        }

        public ScenarioData GenerateScenario(GameContext context, LevelNarrative levelNarrative)
        {
            var scenario = new ScenarioData
            {
                DifficultyLevel = CalculateDifficulty(context),
                Theme = SelectTheme(context),
                EstimatedPlatformCount = CalculatePlatformCount(context)
            };

            scenario.RequiredPlatforms = GeneratePlatformRequirements(context, scenario, levelNarrative);
            return scenario;
        }

        private int CalculateDifficulty(GameContext context)
        {
            return 10;
        }

        private LevelTheme SelectTheme(GameContext context)
        {
            var themes = System.Enum.GetValues(typeof(LevelTheme)).Cast<LevelTheme>().ToArray();
            return themes[context.StoryState % themes.Length];
        }

        private int CalculatePlatformCount(GameContext context)
        {
            return 5 + context.CharacterLevel;
        }

        private List<PlatformRequirement> GeneratePlatformRequirements(
            GameContext context,
            ScenarioData scenario,
            LevelNarrative levelNarrative)
        {
            var requirements = new List<PlatformRequirement>();

            // Start with a simple platform; it may carry discoverable loot.
            requirements.Add(new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = WithPlatformLoot(
                    new List<PlatformContentType> { PlatformContentType.None },
                    scenario.Theme,
                    requirements.Count)
            });

            // Add NPC platforms from narrative assignments
            if (levelNarrative?.Assignments != null)
            {
                Debug.Log($"[ScenarioGenerator] Creating {levelNarrative.Assignments.Count} NPC platforms");

                foreach (var assignment in levelNarrative.Assignments)
                {
                    var storyData = new StoryPlatformData
                    {
                        NpcId = assignment.Npc.NpcId,
                        DialogueKnot = assignment.Story?.StartingKnot,
                        IsKeyNode = assignment.HasStory,
                        NpcCanBecomeEnemy = assignment.Npc.CanBecomeEnemy,
                        EnemyId = assignment.Npc.EnemyDefinition?.EnemyId.ToString()
                    };

                    requirements.Add(new PlatformRequirement
                    {
                        Type = PlatformType.Simple,
                        ContentTypes = new List<PlatformContentType> { PlatformContentType.Npc },
                        StoryData = storyData
                    });
                }
            }

            // Add combat platforms based on difficulty; loot presence is rolled
            // per platform with the biome-configured probability.
            int combatCount = scenario.DifficultyLevel;
            for (int i = 0; i < combatCount; i++)
            {
                requirements.Add(new PlatformRequirement
                {
                    Type = PlatformType.Combat,
                    ContentTypes = WithPlatformLoot(
                        new List<PlatformContentType> { PlatformContentType.Enemy },
                        scenario.Theme,
                        requirements.Count)
                });
            }

            return requirements;
        }

        private List<PlatformContentType> WithPlatformLoot(
            List<PlatformContentType> contentTypes,
            LevelTheme theme,
            int platformIndex)
        {
            // Requirement order matches graph node ids (PlatformGraphGenerator assigns
            // sequential ids), so the index keeps loot placement run-deterministic.
            if (_lootRollService.ShouldPlaceLootOnPlatform(theme, platformIndex))
            {
                contentTypes.Add(PlatformContentType.Loot);
            }

            return contentTypes;
        }
    }
}
