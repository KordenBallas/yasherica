// Example usage of the new architecture
// This file shows how to use the system in Unity

using UnityEngine;
using LevelGeneration;
using Platform;
using Zenject;

public class AreaGenerationExample : MonoBehaviour
{
    [Header("Generation Settings")]
    public int seed = 12345;
    public float noiseScale = 0.1f;
    public int noiseOctaves = 4;
    
    [Header("Character Settings")]
    public Transform characterTransform;
    
    private IAreaGenerator areaGenerator;
    private AreaView areaView;
    private IPlatform currentPlatform;
    
    void Start()
    {
        // 1. Create game context
        var gameContext = new GameContext
        {
            CharacterLevel = 5,
            Progress = 100,
            StoryState = 1
        };
        
        // 2. Generate scenario
        var scenarioGenerator = new ScenarioGenerator();
        var scenario = scenarioGenerator.GenerateScenario(gameContext);
        
        // 3. Generate platform graph
        var graphGenerator = new PlatformGraphGenerator();
        var graph = graphGenerator.GenerateGraph(scenario);
        
        // 4. Create noise map
        var noiseMap = new PerlinNoiseMap(seed, noiseScale, noiseOctaves);
        
        // 5. Create area generator
        areaGenerator = new AreaGenerator(graph, noiseMap);
        areaGenerator.Generate();
        
        // 6. Set up AreaView
        areaView = gameObject.AddComponent<AreaView>();
        areaView.Initialize(areaGenerator);
        
        // 7. Place character at entry platform
        if (areaGenerator.EntryPlatform != null && characterTransform != null)
        {
            currentPlatform = areaGenerator.EntryPlatform;
            characterTransform.position = currentPlatform.Visual.Position + Vector3.up * 2f;
        }
    }
    
    // Call this when character moves to a new platform
    public void OnCharacterMovedToPlatform(IPlatform newPlatform)
    {
        if (newPlatform != null && newPlatform != currentPlatform)
        {
            currentPlatform = newPlatform;
            areaView.OnCharacterPlatformChanged(newPlatform);
        }
    }
    
    // Example: Using builder pattern for custom graph
    void CreateCustomGraph()
    {
        var graph = new PlatformGraphBuilder()
            .WithPlatform(
                PlatformDefinitionBuilder.NewInstance()
                    .WithType(PlatformType.Simple)
                    .WithContent(PlatformContentType.Npc)
                    .WithContent(PlatformContentType.Quest)
            )
            .WithPlatform(
                PlatformDefinitionBuilder.NewInstance()
                    .WithType(PlatformType.Combat)
                    .WithContent(PlatformContentType.Enemy)
                    .WithContent(PlatformContentType.Loot)
            )
            .Build();
        
        var noiseMap = new PerlinNoiseMap(seed, noiseScale, noiseOctaves);
        var areaGen = new AreaGenerator(graph, noiseMap);
        areaGen.Generate();
    }
}

