using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Generation;
using UnityEngine;

namespace LevelGeneration
{
    public class ScenarioGenerator : IScenarioGenerator
    {
        private readonly IStoryPolicyProvider _policyProvider;

        /// <summary>
        /// Creates a new scenario generator.
        /// </summary>
        /// <param name="policyProvider">Optional policy provider for story count calculation.
        /// If null, creates a default provider.</param>
        public ScenarioGenerator(IStoryPolicyProvider policyProvider = null)
        {
            _policyProvider = policyProvider ?? new StoryPolicyProvider();
        }

        public ScenarioData GenerateScenario(GameContext context)
        {
            var scenario = new ScenarioData
            {
                DifficultyLevel = CalculateDifficulty(context),
                Theme = SelectTheme(context),
                EstimatedPlatformCount = CalculatePlatformCount(context)
            };
            
            // Generate required platforms based on context
            scenario.RequiredPlatforms = GeneratePlatformRequirements(context, scenario);
            
            return scenario;
        }
        
        private int CalculateDifficulty(GameContext context)
        {
            // Simple difficulty calculation based on character level
            return 10; // Mathf.Max(1, context.CharacterLevel / 5);
        }
        
        private LevelTheme SelectTheme(GameContext context)
        {
            // Simple theme selection (can be more complex)
            var themes = System.Enum.GetValues(typeof(LevelTheme)).Cast<LevelTheme>().ToArray();
            return themes[context.StoryState % themes.Length];
        }
        
        private int CalculatePlatformCount(GameContext context)
        {
            // Base count + scaling with level
            return 5 + context.CharacterLevel;
        }
        
        private List<PlatformRequirement> GeneratePlatformRequirements(GameContext context, ScenarioData scenario)
        {
            var requirements = new List<PlatformRequirement>();

            // Always start with a simple platform
            requirements.Add(new PlatformRequirement
            {
                Type = PlatformType.Simple,
                ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
            });

            // Add combat platforms based on difficulty
            int combatCount = scenario.DifficultyLevel;
            for (int i = 0; i < combatCount; i++)
            {
                requirements.Add(new PlatformRequirement
                {
                    Type = PlatformType.Combat,
                    ContentTypes = new List<PlatformContentType>
                    {
                        PlatformContentType.Enemy,
                        PlatformContentType.Loot
                    }
                });
            }

            // Calculate expected story count using policy provider
            // This ensures NPC platform count matches the stories NarrativeGenerator will create
            if (context.StoryState > 0)
            {
                int totalPlatforms = scenario.EstimatedPlatformCount;
                int mainStoryCount = 1; // Always 1 main story
                int sideStoryCount = _policyProvider.CalculateSideStoryCount(totalPlatforms);
                int totalStories = mainStoryCount + sideStoryCount;

                // Create NPC platforms for each story
                for (int i = 0; i < totalStories; i++)
                {
                    var npcPlatform = new PlatformRequirement
                    {
                        Type = PlatformType.Simple,
                        ContentTypes = new List<PlatformContentType>
                        {
                            PlatformContentType.Npc,
                            PlatformContentType.Quest
                        }
                    };

                    // Mark first NPC platform as key (for main story binding)
                    if (i == 0)
                    {
                        npcPlatform.StoryData = new StoryPlatformData
                        {
                            IsKeyNode = true
                        };
                    }

                    requirements.Add(npcPlatform);
                }
            }

            return requirements;
        }
    }
}

