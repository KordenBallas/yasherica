using System;
using UnityEngine;
using LevelGeneration;
using Platform;
using Combat.Core;
using Combat.Player;
using CharacterSystem.Runtime;
using Core.Logging;
using Narrative.Actors.Data;
using Narrative.Casting.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Interaction;
using Narrative.Interaction.Core;
using Zenject;

public class AreaSceneEntrypoint : MonoBehaviour, IInitializable, IDisposable
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

    private AreaGenerator areaGenerator;
    private RunStreamingCoordinator coordinator;
    private AreaView areaView;
    private IPlatform currentPlatform;

    [Inject]
    private Platform.Platform.Factory _platformFactory;
    [Inject]
    private IPlayerRegistry _playerRegistry;
    [Inject]
    private DiContainer _container;
    [Inject]
    private Loot.Core.IRunSeedProvider _runSeedProvider;
    [Inject]
    private Loot.Core.ILootRollService _lootRollService;
    [Inject]
    private Loot.Core.ICurrentThemeProvider _currentThemeProvider;
    [Inject]
    private IRunWindowPlanner _windowPlanner;
    [Inject]
    private INpcArchetypeCatalog _archetypeCatalog;
    [Inject]
    private IModularCharacterFactory _modularFactory;
    [Inject]
    private IFactStore _factStore;
    [Inject]
    private ICastingFactory _castingFactory;
    [Inject]
    private IFragmentLibrary _fragmentLibrary;
    [Inject]
    private NpcIntentResolver _intentResolver;
    [Inject]
    private INpcInteractionService _interactionService;
    [Inject]
    private IGameLogger _logger;

    private IPlayer _localPlayer;

    public void Initialize()
    {
        _localPlayer = new HumanPlayer(id: 1, name: "Player");
        _playerRegistry.RegisterLocalPlayer(_localPlayer);
        _logger?.Info(LogCategory.Area,"[AreaSceneEntrypoint] Created and registered local player");

        GenerateArea();
    }

    public void GenerateArea()
    {
        // Effective seed is computed once at install time (LootInstaller) so loot rolls, the windowed
        // planner, and the layout share the same run seed.
        UnityEngine.Random.InitState(_runSeedProvider.RunSeed);

        // Biome selection from progression is a follow-up; default for now so loot/theme have a value.
        LevelTheme theme = LevelTheme.Forest;
        _currentThemeProvider.SetTheme(theme);

        var noiseMap = new PerlinNoiseMap(seed, noiseScale, noiseOctaves);

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

        // The streaming director plans/generates platforms window-by-window; the area generator no longer
        // needs a pre-built graph or pre-assigned narrative (levelNarrative is null on this path).
        areaGenerator = new AreaGenerator(
            new PlatformGraphData(), noiseMap, _platformFactory, _lootRollService, theme, config, _logger);

        coordinator = new RunStreamingCoordinator(
            _windowPlanner, _archetypeCatalog, _modularFactory, _factStore, _castingFactory,
            _fragmentLibrary, _intentResolver, _interactionService, areaGenerator, _logger);

        IPlatform entry = coordinator.Begin();

        if (areaView == null)
        {
            areaView = gameObject.AddComponent<AreaView>();
        }
        areaView.Initialize(areaGenerator);

        if (entry != null && characterTransform != null)
        {
            currentPlatform = entry;
            characterTransform.position = entry.Visual.Position + Vector3.up * 2f;

            var characterController = characterTransform.GetComponent<Character.CharacterMovementController>();
            if (characterController == null)
            {
                _logger?.Warning(LogCategory.Area,"[AreaSceneEntrypoint] CharacterTransform does not have CharacterMovementController component.");
            }
            else
            {
                _container.Inject(characterController);
                _logger?.Info(LogCategory.Area,"[AreaSceneEntrypoint] Injected dependencies into CharacterMovementController");
            }
        }

        // Content spawner (instantiated through the container so its injected dependencies resolve).
        var contentSpawner = gameObject.GetComponent<Platform.ContentSpawner>();
        if (contentSpawner == null)
        {
            contentSpawner = _container.InstantiateComponent<Platform.ContentSpawner>(gameObject);
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

    public void Dispose()
    {
        coordinator?.Dispose();
    }
}
