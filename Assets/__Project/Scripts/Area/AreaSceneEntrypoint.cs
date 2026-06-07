using UnityEngine;
using LevelGeneration;
using Narrative.Data.Definitions;
using Narrative.Generation;
using Platform;
using Combat.Core;
using Combat.Player;
using Zenject;

public class AreaSceneEntrypoint : MonoBehaviour, IInitializable
{
    [Header("Graph Parameters")]
    [Tooltip("Number of platforms (nodes) in the route graph")]
    public int platformCount = 6;
    [Tooltip("Gap between neighboring platforms (world units)")]
    public float gapBetweenPlatforms = 2.0f;
    [Tooltip("Maximum absolute height deviation between consecutive platforms")]
    public float heightDeviation = 1.5f;
    [Tooltip("Deterministic seed. 0 uses random seed.")]
    public int seed = 0;

    [Header("Platform Shape")]
    [Tooltip("Min/Max size (length X, width Z) for platforms")]
    public Vector2 platformSizeMin = new Vector2(3f, 2f);
    public Vector2 platformSizeMax = new Vector2(6f, 4f);
    [Tooltip("Number of vertices around platform top edge (6..24). More -> more detailed jagged edge")]
    [Range(6, 24)] public int edgeVertexCount = 10;
    [Tooltip("Amount of jitter applied to the top edge in world units (relative to scale)")]
    [Range(0f, 0.8f)] public float edgeJitter = 0.25f;
    [Tooltip("Thickness of platform (height downwards from top) in world units")]
    public float platformThickness = 1.0f;

    [Header("Appearance")]
    public Material platformMaterial; // optional - if null a default will be created
    [Tooltip("Whether to color platforms with varying tint")]
    public bool colorVariation = true;

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    public int noiseOctaves = 4;

    [Header("Character Settings")]
    public Transform characterTransform;

    private IAreaGenerator areaGenerator;
    private AreaView areaView;
    private IPlatform currentPlatform;

    [Inject]
    private Platform.Platform.Factory _platformFactory;
    [Inject]
    private IPlayerRegistry _playerRegistry;
    [Inject]
    private DiContainer _container;
    [Inject]
    private ILevelNarrativeGenerator _narrativeGenerator;
    [Inject]
    private LevelNarrativeConfig _levelConfig;
    [Inject]
    private IScenarioGenerator _scenarioGenerator;

    private IPlayer _localPlayer;

    public void Initialize()
    {
        _localPlayer = new HumanPlayer(id: 1, name: "Player");
        _playerRegistry.RegisterLocalPlayer(_localPlayer);
        Debug.Log("[AreaSceneEntrypoint] Created and registered local player");

        GenerateArea();
    }

    public void GenerateArea()
    {
        if (seed != 0)
        {
            Random.InitState(seed);
        }
        else
        {
            Random.InitState((int)System.DateTime.Now.Ticks & 0x0000FFFF);
        }

        // 1. Generate narrative content (NPC assignments)
        var levelNarrative = _narrativeGenerator.Generate(_levelConfig);

        // 2. Generate scenario (platform layout)
        var gameContext = new GameContext
        {
            CharacterLevel = 5,
            Progress = 100,
            StoryState = 1
        };
        var scenario = _scenarioGenerator.GenerateScenario(gameContext, levelNarrative);
        if (scenario == null)
        {
            Debug.LogError("[AreaSceneEntrypoint] Failed to generate scenario");
            return;
        }

        if (platformCount > 0)
        {
            scenario.EstimatedPlatformCount = platformCount;
        }

        // 3. Generate platform graph
        var graphGenerator = new PlatformGraphGenerator();
        var graph = graphGenerator.GenerateGraph(scenario);

        // 4. Create noise map
        var noiseMap = new PerlinNoiseMap(seed, noiseScale, noiseOctaves);

        // 5. Create configuration
        var config = new AreaGeneratorConfig
        {
            platformSizeMin = platformSizeMin,
            platformSizeMax = platformSizeMax,
            edgeVertexCount = edgeVertexCount,
            edgeJitter = edgeJitter,
            platformThickness = platformThickness,
            gapBetweenPlatforms = gapBetweenPlatforms,
            heightDeviation = heightDeviation,
            platformMaterial = platformMaterial,
            colorVariation = colorVariation
        };

        // 6. Create area generator with narrative data
        areaGenerator = new AreaGenerator(graph, noiseMap, _platformFactory, levelNarrative, config);
        areaGenerator.Generate();

        // 7. Set up AreaView
        if (areaView == null)
        {
            areaView = gameObject.AddComponent<AreaView>();
        }
        areaView.Initialize(areaGenerator);

        // 8. Place character at entry platform
        if (areaGenerator.EntryPlatform != null && characterTransform != null)
        {
            currentPlatform = areaGenerator.EntryPlatform;
            characterTransform.position = currentPlatform.Visual.Position + Vector3.up * 2f;

            var characterController = characterTransform.GetComponent<Character.CharacterMovementController>();
            if (characterController == null)
            {
                Debug.LogWarning("[AreaSceneEntrypoint] CharacterTransform does not have CharacterMovementController component.");
            }
            else
            {
                _container.Inject(characterController);
                Debug.Log("[AreaSceneEntrypoint] Injected dependencies into CharacterMovementController");
            }
        }

        // 9. Set up content spawner
        var contentSpawner = gameObject.GetComponent<Platform.ContentSpawner>();
        if (contentSpawner == null)
        {
            contentSpawner = gameObject.AddComponent<Platform.ContentSpawner>();
        }
    }

    [ContextMenu("Clear Area")]
    public void ClearArea()
    {
        if (areaGenerator != null)
        {
            areaGenerator.Clear();
        }
    }

    public void OnCharacterMovedToPlatform(IPlatform newPlatform)
    {
        if (newPlatform != null && newPlatform != currentPlatform)
        {
            currentPlatform = newPlatform;
            if (areaView != null)
            {
                areaView.OnCharacterPlatformChanged(newPlatform);
            }
        }
    }
}
