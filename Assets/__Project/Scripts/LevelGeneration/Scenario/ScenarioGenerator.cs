using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LevelGeneration
{
    public class ScenarioGenerator : IScenarioGenerator
    {
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
            
            // Add some NPC/Quest platforms
            if (context.StoryState > 0)
            {
                requirements.Add(new PlatformRequirement
                {
                    Type = PlatformType.Simple,
                    ContentTypes = new List<PlatformContentType> 
                    { 
                        PlatformContentType.Npc,
                        PlatformContentType.Quest 
                    }
                });
            }
            
            return requirements;
        }
    }
}

