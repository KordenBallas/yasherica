using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using LevelGeneration.Route;
using LevelGeneration.Surface;
using Loot.Core;
using Narrative.Director.Core;
using Platform;
using UnityEngine;
using World.Dressing.Core;
using Zenject;

namespace LevelGeneration
{
    /// <summary>
    /// Generates area with platforms from graph data.
    /// Uses unified Platform.Factory - state behavior is content-driven.
    /// Each platform's hex surface is grown deterministically from a per-platform seed derived from
    /// the run seed and the node id, sized by its content kind's shape profile (brief §5–§8).
    /// </summary>
    public class AreaGenerator : IAreaGenerator
    {
        /// <summary>Seed-context prefix for the per-platform shape stream (kept apart from loot/director streams).</summary>
        private const string ShapeSeedContext = "platform-shape";

        private readonly PlatformGraphData _graph;
        private readonly RunRouteModel _routeModel;
        private readonly AreaGeneratorConfig _config;
        private readonly Platform.Platform.Factory _platformFactory;
        private readonly ILootRollService _lootRollService;
        private readonly ICurrentThemeProvider _themeProvider;
        private readonly PlatformShapeSettings _shapeSettings;
        private readonly IRunSeedProvider _seedProvider;
        private readonly PlatformSurfaceGenerator _surfaceGenerator = new();
        private readonly IGameLogger _logger;
        private readonly IRouteLandmarkSpawner _landmarkSpawner;
        private readonly IEnvironmentDressingPlanner _dressingPlanner;
        private readonly IEnvironmentDressingSpawner _dressingSpawner;

        private readonly Dictionary<int, IPlatform> _platforms = new();
        private readonly Dictionary<int, PlatformView> _platformViews = new();
        private readonly Dictionary<int, PlatformDressingPlan> _pendingDressing = new();
        private IPlatform _entryPlatform;
        private IPlatform _lastAppended;
        private GameObject _areaGameObject;
        private float _cursorX = 0f;
        private float _landmarkScanX = 0f;

        public IPlatform EntryPlatform => _entryPlatform;

        /// <summary>Looks up a generated platform by its node id (P2-2 continue: hero re-placement).</summary>
        public bool TryGetPlatform(int nodeId, out IPlatform platform) => _platforms.TryGetValue(nodeId, out platform);

        public AreaGenerator(
            PlatformGraphData graph,
            RunRouteModel routeModel,
            Platform.Platform.Factory platformFactory,
            ILootRollService lootRollService,
            ICurrentThemeProvider themeProvider,
            PlatformShapeSettings shapeSettings,
            IRunSeedProvider seedProvider,
            AreaGeneratorConfig config = null,
            IGameLogger logger = null,
            IRouteLandmarkSpawner landmarkSpawner = null,
            IEnvironmentDressingPlanner dressingPlanner = null,
            IEnvironmentDressingSpawner dressingSpawner = null)
        {
            _graph = graph;
            _routeModel = routeModel ?? new RunRouteModel(BiomeLandscapeSettings.CreateDefault(), 0);
            _platformFactory = platformFactory;
            _lootRollService = lootRollService;
            _themeProvider = themeProvider;
            _shapeSettings = shapeSettings ?? PlatformShapeSettings.CreateDefault();
            _seedProvider = seedProvider;
            _config = config ?? new AreaGeneratorConfig();
            _logger = logger;
            _landmarkSpawner = landmarkSpawner;
            _dressingPlanner = dressingPlanner;
            _dressingSpawner = dressingSpawner;
        }

        /// <summary>
        /// One-shot generation of the whole graph (legacy path). Equivalent to
        /// <see cref="Initialize"/> followed by <see cref="AppendPlatforms"/> over every graph node — the
        /// same seam the streaming director drives window-by-window.
        /// </summary>
        public void Generate()
        {
            Initialize();
            AppendPlatforms(_graph.Nodes);

            _logger?.Info(LogCategory.LevelGeneration,$"[AreaGenerator] Created {_platformViews.Count} platform GameObjects");

            if (_entryPlatform == null)
            {
                _logger?.Warning(LogCategory.LevelGeneration,"[AreaGenerator] No entry platform found!");
            }
        }

        /// <summary>
        /// Prepares an empty area: clears any existing platforms and resets the layout cursor. Platforms
        /// are added afterwards via <see cref="AppendPlatforms"/> (one window at a time when streaming).
        /// </summary>
        public void Initialize()
        {
            Clear();
            _cursorX = 0f;
            _landmarkScanX = 0f;
            _lastAppended = null;
            CreateAreaGameObject();
        }

        /// <summary>
        /// Appends platforms for the given nodes onto the running layout, linking each to the previously
        /// appended platform (a linear chain — branch edges are not used; the brancher is disabled). The
        /// layout cursor persists across calls so successive windows lay out end to end. The first platform
        /// ever appended becomes the entry platform.
        /// </summary>
        public void AppendPlatforms(IReadOnlyList<GraphNode> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            if (_areaGameObject == null)
            {
                Initialize();
            }

            foreach (var node in nodes)
            {
                IPlatform platform = CreatePlatformFromNode(node);
                if (platform == null)
                {
                    continue;
                }

                _platforms[node.Id] = platform;

                if (_lastAppended != null)
                {
                    _lastAppended.AddNeighbor(platform);
                    platform.AddNeighbor(_lastAppended);
                }

                _lastAppended = platform;
                _entryPlatform ??= platform;

                CreatePlatformGameObject(platform);
            }

            SpawnPendingLandmarks();
        }

        /// <summary>
        /// Places the routing landmarks whose arc apex falls in the forward span this window just
        /// laid out. The scan cursor persists across windows like <see cref="_cursorX"/>, and the
        /// half-open range guarantees each landmark spawns exactly once while streaming.
        /// </summary>
        private void SpawnPendingLandmarks()
        {
            if (_landmarkSpawner == null || _areaGameObject == null || _cursorX <= _landmarkScanX)
            {
                return;
            }

            var specs = _routeModel.GetLandmarksInRange(_landmarkScanX, _cursorX);
            if (specs.Count > 0)
            {
                _landmarkSpawner.Spawn(specs, _areaGameObject.transform);
            }

            _landmarkScanX = _cursorX;
        }

        public void Clear()
        {
            // Unregister all platforms from registry
            var registry = PlatformRegistry.Instance;
            if (registry != null)
            {
                foreach (var platform in _platforms.Values)
                {
                    registry.UnregisterPlatform(platform);
                }
            }

            // Destroy all platform GameObjects
            foreach (var view in _platformViews.Values)
            {
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
            _platformViews.Clear();

            // Destroy area GameObject
            if (_areaGameObject != null)
            {
                Object.Destroy(_areaGameObject);
                _areaGameObject = null;
            }

            _platforms.Clear();
            _pendingDressing.Clear();
            _entryPlatform = null;
        }

        private void CreateAreaGameObject()
        {
            _areaGameObject = new GameObject("Area");
        }

        private IPlatform CreatePlatformFromNode(GraphNode node)
        {
            // Create unified platform - state factory handles the rest based on content
            IPlatform platform = _platformFactory.Create(node.Id);

            // Set story data BEFORE adding content (content might need it during Initialize)
            if (node.StoryData != null)
            {
                platform.SetStoryData(node.StoryData);
            }

            // Add content (determines which states activate). Planner-built content (the streaming
            // narrative path) takes precedence over creating content from the node's content types.
            if (node.PrebuiltContent != null && node.PrebuiltContent.Count > 0)
            {
                foreach (var content in node.PrebuiltContent)
                {
                    if (content != null)
                    {
                        platform.AddContent(content);
                    }
                }
            }
            else
            {
                foreach (var contentType in node.ContentTypes)
                {
                    var content = CreateContent(contentType, node);
                    if (content != null)
                    {
                        platform.AddContent(content);
                    }
                }
            }

            // Grow the hex surface deterministically: per-platform seed, content-kind profile, and
            // the battlefield-minimum floor for anything that can host a fight (brief §5–§8).
            var rng = CreatePlatformRng(node.Id);
            PlatformContentKind kind = node.ShapeKindOverride ?? PlatformContentKindResolver.Resolve(node);
            var profile = _shapeSettings.ProfileFor(kind);
            int guaranteedMinCells = kind == PlatformContentKind.Combat ? _shapeSettings.BattlefieldMinimumCells : 0;
            var surface = _surfaceGenerator.Generate(
                profile, guaranteedMinCells, _shapeSettings.HexSize, _shapeSettings.Orientation,
                _shapeSettings.RimWidth, _shapeSettings.RimJitterPercent, rng);

            // Environment dressing (data-driven, deterministic): plan the platform's decoration and
            // fold blocking features into the surface BEFORE the mesh/grid consume it. No planner
            // bound (or nothing to dress) = the base layer — generation is unchanged.
            if (_dressingPlanner != null)
            {
                var dressingPlan = _dressingPlanner.Plan(
                    node.Id, node.Site, kind, surface, guaranteedMinCells);
                if (dressingPlan.BlockedCells.Count > 0)
                {
                    surface = surface.WithBlockedCells(dressingPlan.BlockedCells);
                }

                _pendingDressing[node.Id] = dressingPlan;
            }

            var visual = new PlatformVisual();
            visual.Surface = surface;
            // Surface.Outline is already stitched by the generator (notch fills keep the floor
            // continuous under the sewn spans); walls and boundary tests follow it as-is.
            visual.TopBoundary = surface.Outline.Select(p => new Vector3(p.X, 0f, p.Z)).ToList();
            visual.Size = CalculateOutlineBounds(surface);
            visual.Position = CalculatePlatformPosition(visual.Size.x);

            // Initialize with visual (will also initialize content and state machine)
            platform.Initialize(visual);

            return platform;
        }

        private IRandomSource CreatePlatformRng(int nodeId)
        {
            // A private per-platform stream: order-independent across windows and decoupled from the
            // director's shared stream, so shape draws never shift narrative picks.
            int runSeed = _seedProvider?.RunSeed ?? 0;
            int seed = LootSeed.Derive(runSeed, $"{ShapeSeedContext}:{nodeId}");
            return new DeterministicRandom(unchecked((ulong)seed));
        }

        private static Vector2 CalculateOutlineBounds(Combat.Battlefield.PlatformHexSurface surface)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var (x, z) in surface.Outline)
            {
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minZ = Mathf.Min(minZ, z);
                maxZ = Mathf.Max(maxZ, z);
            }

            return new Vector2(maxX - minX, maxZ - minZ);
        }

        private Vector3 CalculatePlatformPosition(float platformWidth)
        {
            // The route model is a pure function of forward distance: X marches with the layout
            // cursor (monotonic — forward always reads onward), Z is the bounded routed-path weave,
            // Y is the elevation tier. Layout/read only; gaps stay clean hops.
            float posX = _cursorX + platformWidth * 0.5f;
            RouteSample sample = _routeModel.Sample(posX);

            _cursorX += platformWidth + _shapeSettings.GapBetweenPlatforms;

            return new Vector3(posX, sample.TierY, sample.LateralZ);
        }

        private IPlatformContent CreateContent(PlatformContentType contentType, GraphNode node)
        {
            switch (contentType)
            {
                case PlatformContentType.Enemy:
                    return CreateEnemyContent(node.StoryData);

                // NPC platforms are produced by the streaming planner as prebuilt NpcContent
                // (with a minted actor + story), never created here from content types.

                case PlatformContentType.Loot:
                    return CreateLootContent(node.Id, node.ContentFlavor);

                case PlatformContentType.Quest:
                    return new QuestContent();

                default:
                    return null;
            }
        }

        private LootContent CreateLootContent(int nodeId, string flavor)
        {
            // A site loot beat's flavor (market/stash/chest/relic) rides in as a bias tag: entries in
            // the biome table carrying the tag get their weight multiplied (the loot layer's existing
            // BiasTags machinery) — direction, not a dedicated table.
            var tags = string.IsNullOrEmpty(flavor) ? null : new[] { flavor };
            // Read the theme live, not a frozen ctor copy: the biome journey advances the provider
            // per stretch, and platform loot must roll the active biome's table.
            var context = new LootRollContext(_themeProvider.CurrentTheme, $"platform:{nodeId}", tags);
            var items = _lootRollService.RollPlatformLoot(context);
            if (items.Count == 0)
            {
                return null;
            }

            _logger?.Info(LogCategory.LevelGeneration,$"[AreaGenerator] Rolled {items.Count} loot item(s) for platform {nodeId} " +
                      $"({string.Join(", ", items.Select(i => i.ArtifactId))})");
            return new LootContent(items);
        }

        private EnemyContent CreateEnemyContent(StoryPlatformData storyData)
        {
            var enemyContent = new EnemyContent();

            if (storyData != null && !string.IsNullOrEmpty(storyData.EnemyId))
            {
                // Parse EnemyId - stored as string but EnemyContent expects int
                if (int.TryParse(storyData.EnemyId, out int enemyId))
                {
                    enemyContent.EnemyId = enemyId;
                }
                else
                {
                    _logger?.Warning(LogCategory.LevelGeneration,$"[AreaGenerator] Invalid EnemyId format: '{storyData.EnemyId}'");
                }
            }

            return enemyContent;
        }

        private void CreatePlatformGameObject(IPlatform platform)
        {
            if (platform == null || _areaGameObject == null) return;

            // Create GameObject for platform
            var platformGO = new GameObject($"Platform_{platform.Id}");
            platform.Visual.GameObject = platformGO; // TODO: refactor this
            platformGO.transform.SetParent(_areaGameObject.transform);
            platformGO.transform.position = platform.Visual.Position;

            // Add PlatformView component
            var platformView = platformGO.AddComponent<PlatformView>();

            // The dressing plan made for this node (if any): its kit may re-ground the platform,
            // and its placements spawn under the platform transform after the view initializes.
            _pendingDressing.TryGetValue(platform.Id, out var dressingPlan);
            Material groundMaterial = dressingPlan != null && _dressingSpawner != null
                ? _dressingSpawner.ResolveGroundMaterial(dressingPlan)
                : null;

            // A dressed ground is the biome's look — the debug per-platform color variation must
            // not stomp it (PlatformView overwrites the material color when a tint is passed).
            Color? debugTint = groundMaterial == null && _config.colorVariation
                ? GetPlatformColor(platform.Id)
                : null;

            // Configure PlatformView with config
            platformView.SetConfig(
                groundMaterial != null ? groundMaterial : _config.platformMaterial,
                _shapeSettings.PlatformThickness,
                _shapeSettings.RimDropHeight,
                _shapeSettings.CellInset,
                debugTint
            );

            platformView.Initialize(platform);

            if (dressingPlan != null && _dressingSpawner != null)
            {
                _dressingSpawner.Spawn(dressingPlan, platformGO.transform);
            }

            // Store the view
            _platformViews[platform.Id] = platformView;

            // Register with PlatformRegistry
            var registry = PlatformRegistry.Instance;
            if (registry != null)
            {
                registry.RegisterPlatform(platform, platformView);
            }

            // All platforms are active (no pooling)
            platformGO.SetActive(true);
        }

        private Color? GetPlatformColor(int platformId)
        {
            if (!_config.colorVariation) return null;

            int totalPlatforms = _platforms.Count;
            float hue = (platformId / (float)Mathf.Max(1, totalPlatforms)) * 0.6f;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }
    }
}
