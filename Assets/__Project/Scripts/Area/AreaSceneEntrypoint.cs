using System;
using UnityEngine;
using LevelGeneration;
using LevelGeneration.Journey;
using LevelGeneration.Route;
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
using World.Biomes;
using World.Biomes.Data;
using World.Landscape;
using Zenject;

public class AreaSceneEntrypoint : MonoBehaviour, IInitializable, IDisposable, IBiomeStretchObserver
{
    [Header("Graph Parameters")]
    [Tooltip("Number of platforms (nodes) in the route graph")]
    public int platformCount = 6;
    [Tooltip("Route seed override (layout weave/tiers/landmarks). 0 derives it from the run seed.")]
    public int seed = 0;

    // Platform size/shape/layout dials live on the PlatformShapeConfig SO
    // (Resources/LevelGeneration/PlatformShapeConfig), wired through AreaInstaller.

    [Header("Appearance")]
    public Material platformMaterial; // optional - if null a default will be created
    [Tooltip("Whether to color platforms with varying tint")]
    public bool colorVariation = true;

    [Header("Character Settings")]
    public Transform characterTransform;

    private AreaGenerator areaGenerator;
    private RunStreamingCoordinator coordinator;
    private AreaView areaView;
    private IPlatform currentPlatform;
    private GameObject worldBackdrop;
    private RouteLandmarkSpawner _landmarkSpawner;
    private int _routeSeed;
    private LevelTheme _builtBiomeTheme;

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
    private IBiomeJourney _biomeJourney;
    [Inject]
    private IRunWindowPlanner _windowPlanner;
    [Inject]
    private INpcArchetypeCatalog _archetypeCatalog;
    [Inject]
    private IModularCharacterFactory _modularFactory;
    [Inject]
    private CharacterSystem.Runtime.IDemoRoleTintApplier _tintApplier;
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
    private LevelGeneration.Surface.PlatformShapeSettings _platformShapeSettings;
    [Inject]
    private IBiomeAppearanceCatalog _biomeAppearanceCatalog;
    [Inject]
    private Core.Camera.CameraConfig _cameraConfig;
    [Inject]
    private IGameLogger _logger;
    [Inject]
    private Narrative.Actors.Core.ILiveActorRegistry _actorRegistry;
    [Inject]
    private System.Collections.Generic.IReadOnlyList<Narrative.Stories.Core.StoryTemplateData> _storyTemplates;
    [Inject]
    private Core.Persistence.RunRestoreContext _restoreContext;
    [Inject]
    private LevelGeneration.WorldStatePersistenceBridge _worldPersistence;
    [Inject]
    private Core.Persistence.AutosaveService _autosave;
    [Inject]
    private World.Dressing.Core.IEnvironmentDressingPlanner _dressingPlanner;
    [Inject]
    private IEnvironmentDressingSpawner _dressingSpawner;
    [Inject]
    private IBackdropScatterSpawner _backdropScatterSpawner;

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

        // The run's initial biome is the journey's window-0 stretch (seeded, tier-ordered — no more
        // hardcoded Forest). The theme provider is NOT set here: the BiomeStretchDirector owns all
        // SetTheme/fact writes and applies window 0 inside coordinator.Begin(), before any theme read.
        LevelTheme theme = _biomeJourney.ForWindow(0).Theme;
        _builtBiomeTheme = theme;

        // The routed-path model (weave / elevation tiers / landmark placement) is run-deterministic:
        // its seed derives from the run seed unless overridden, and its character comes from the
        // biome's appearance asset (code defaults when the biome is unauthored). The route model keeps
        // the entry biome's character for the whole run (known limitation — M5 transition art);
        // landmark dressing and the backdrop DO follow biome stretches via OnBiomeStretchChanged.
        BiomeAppearanceDefinition biomeAppearance = _biomeAppearanceCatalog.Get(theme);
        BiomeLandscapeSettings landscapeSettings = BiomeAppearanceMapper.ToLandscapeSettings(biomeAppearance);
        _routeSeed = seed != 0 ? seed : Loot.Core.LootSeed.Derive(_runSeedProvider.RunSeed, "landscape-route");
        int routeSeed = _routeSeed;
        var routeModel = new RunRouteModel(landscapeSettings, routeSeed);
        _landmarkSpawner = new RouteLandmarkSpawner(biomeAppearance, theme);
        var landmarkSpawner = _landmarkSpawner;

        var config = new AreaGeneratorConfig
        {
            platformMaterial = platformMaterial,
            colorVariation = colorVariation
        };

        // The streaming director plans/generates platforms window-by-window; the area generator no longer
        // needs a pre-built graph or pre-assigned narrative (levelNarrative is null on this path).
        areaGenerator = new AreaGenerator(
            new PlatformGraphData(), routeModel, _platformFactory, _lootRollService, _currentThemeProvider,
            _platformShapeSettings, _runSeedProvider, config, _logger, landmarkSpawner,
            _dressingPlanner, _dressingSpawner, _backdropScatterSpawner);

        CreateWorldBackdrop(biomeAppearance, landscapeSettings, routeSeed);

        // The stretch director owns all theme/tier-fact writes; this entrypoint observes stretch
        // crossings to swap the landmark dressing and rebuild the backdrop.
        var biomeDirector = new BiomeStretchDirector(
            _biomeJourney, _currentThemeProvider, _factStore, observer: this, _logger);

        coordinator = new RunStreamingCoordinator(
            _windowPlanner, _archetypeCatalog, _modularFactory, _factStore, _castingFactory,
            _fragmentLibrary, _intentResolver, _interactionService, areaGenerator, biomeDirector,
            _actorRegistry, _storyTemplates, _logger, _tintApplier);
        _worldPersistence.Attach(coordinator);

        // Continue (P2-2): a valid run save rebuilds the recorded world (windows behind + beats
        // ahead) and returns the platform the hero stood on; a fresh run plans window 0 live. The
        // restore coordinator has already replayed facts/RNG/actors/quests/inventory by now.
        IPlatform entry = _restoreContext.IsRestoring
            ? coordinator.BeginRestored(_restoreContext.Snapshot.World)
            : coordinator.Begin();

        if (areaView == null)
        {
            areaView = gameObject.AddComponent<AreaView>();
        }
        areaView.Initialize(areaGenerator);

        if (entry != null && characterTransform != null)
        {
            currentPlatform = entry;
            // Spawn over the center CELL, not the raw centroid — a concave island's centroid can fall
            // outside every cell.
            characterTransform.position =
                PlatformAnchor.CenterCellWorld(entry.Visual.Surface, entry.Visual.Position) + Vector3.up * 2f;

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

        // The initial savepoint (A1): the run is continuable from the moment the world exists —
        // quitting before crossing a single platform still resumes at the entry platform. On a
        // continue the existing run.json IS the valid savepoint: re-saving here would capture the
        // pre-rig-assembly instant (the hero body applies in Start) and could lose the saved body.
        if (!_restoreContext.IsRestoring)
        {
            _autosave.Save();
        }
    }

    [ContextMenu("Clear Area")]
    public void ClearArea()
    {
        if (areaGenerator != null)
        {
            areaGenerator.Clear();
        }

        DestroyWorldBackdrop();
    }

    /// <summary>
    /// Builds the distant biome horizon behind the whole run and keeps it anchored to the hero so
    /// it reads as infinitely distant under the fixed isometric camera.
    /// </summary>
    private void CreateWorldBackdrop(
        BiomeAppearanceDefinition biomeAppearance, BiomeLandscapeSettings landscapeSettings, int routeSeed)
    {
        DestroyWorldBackdrop();
        if (characterTransform == null)
        {
            return;
        }

        var builder = new WorldBackdropBuilder();
        worldBackdrop = builder.Build(
            biomeAppearance, landscapeSettings, routeSeed,
            _cameraConfig.IsometricRotation.y, _cameraConfig.IsometricRotation.x);
        var backdropView = worldBackdrop.AddComponent<WorldBackdropView>();
        backdropView.Initialize(characterTransform);
    }

    /// <summary>
    /// Biome-stretch crossing (from <see cref="BiomeStretchDirector"/>): swap the landmark dressing
    /// and rebuild the backdrop for the new biome. Already-built route geometry stays — the country
    /// behind the player keeps its look. Idempotent for the window-0 apply (theme already built).
    /// </summary>
    public void OnBiomeStretchChanged(BiomeStretch stretch)
    {
        if (_landmarkSpawner == null || stretch.Theme == _builtBiomeTheme)
        {
            return;
        }

        BiomeAppearanceDefinition appearance = _biomeAppearanceCatalog.Get(stretch.Theme);
        BiomeLandscapeSettings landscape = BiomeAppearanceMapper.ToLandscapeSettings(appearance);
        _landmarkSpawner.ApplyBiome(appearance, stretch.Theme);
        CreateWorldBackdrop(appearance, landscape, _routeSeed);
        _builtBiomeTheme = stretch.Theme;
        _logger?.Info(LogCategory.Area,
            $"[AreaSceneEntrypoint] Crossed into {stretch.Theme} (tier {stretch.EscalationTier}); landmark dressing + backdrop swapped.");
    }

    private void DestroyWorldBackdrop()
    {
        if (worldBackdrop != null)
        {
            Destroy(worldBackdrop);
            worldBackdrop = null;
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
        DestroyWorldBackdrop();
    }
}
