using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using LevelGeneration.Surface;
using Loot.Core;
using Narrative.Director.Core;
using Platform;
using UnityEngine;
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

        /// <summary>Integer resolution of the seeded height-deviation draw.</summary>
        private const int HeightDeviationSteps = 100;

        private readonly PlatformGraphData _graph;
        private readonly PerlinNoiseMap _noiseMap;
        private readonly AreaGeneratorConfig _config;
        private readonly Platform.Platform.Factory _platformFactory;
        private readonly ILootRollService _lootRollService;
        private readonly LevelTheme _theme;
        private readonly PlatformShapeSettings _shapeSettings;
        private readonly IRunSeedProvider _seedProvider;
        private readonly PlatformSurfaceGenerator _surfaceGenerator = new();
        private readonly IGameLogger _logger;

        private readonly Dictionary<int, IPlatform> _platforms = new();
        private readonly Dictionary<int, PlatformView> _platformViews = new();
        private IPlatform _entryPlatform;
        private IPlatform _lastAppended;
        private GameObject _areaGameObject;
        private float _cursorX = 0f;
        private float _baselineY = 0f;

        public IPlatform EntryPlatform => _entryPlatform;

        public AreaGenerator(
            PlatformGraphData graph,
            PerlinNoiseMap noiseMap,
            Platform.Platform.Factory platformFactory,
            ILootRollService lootRollService,
            LevelTheme theme,
            PlatformShapeSettings shapeSettings,
            IRunSeedProvider seedProvider,
            AreaGeneratorConfig config = null,
            IGameLogger logger = null)
        {
            _graph = graph;
            _noiseMap = noiseMap;
            _platformFactory = platformFactory;
            _lootRollService = lootRollService;
            _theme = theme;
            _shapeSettings = shapeSettings ?? PlatformShapeSettings.CreateDefault();
            _seedProvider = seedProvider;
            _config = config ?? new AreaGeneratorConfig();
            _logger = logger;
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
            _baselineY = 0f;
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
                    var content = CreateContent(contentType, node.StoryData, node.Id);
                    if (content != null)
                    {
                        platform.AddContent(content);
                    }
                }
            }

            // Grow the hex surface deterministically: per-platform seed, content-kind profile, and
            // the battlefield-minimum floor for anything that can host a fight (brief §5–§8).
            var rng = CreatePlatformRng(node.Id);
            PlatformContentKind kind = PlatformContentKindResolver.Resolve(node);
            var profile = _shapeSettings.ProfileFor(kind);
            int guaranteedMinCells = kind == PlatformContentKind.Combat ? _shapeSettings.BattlefieldMinimumCells : 0;
            var surface = _surfaceGenerator.Generate(
                profile, guaranteedMinCells, _shapeSettings.HexSize, _shapeSettings.Orientation,
                _shapeSettings.RimWidth, _shapeSettings.RimJitterPercent, rng);

            var visual = new PlatformVisual();
            visual.Surface = surface;
            visual.TopBoundary = surface.Outline.Select(p => new Vector3(p.X, 0f, p.Z)).ToList();
            visual.Size = CalculateOutlineBounds(surface);
            Vector2 position2D = CalculatePlatformPosition(node, visual.Size.x, rng);
            visual.Position = _noiseMap.GetPositionWithHeight(position2D);

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

        private Vector2 CalculatePlatformPosition(GraphNode node, float platformWidth, IRandomSource rng)
        {
            // Calculate position based on previous platforms and gap
            float posX = _cursorX + platformWidth * 0.5f;

            // Height deviation relative to previous node, drawn from the platform's own seeded stream.
            float dy = 0f;
            if (node.Id > 0)
            {
                int step = rng.NextInt(HeightDeviationSteps * 2 + 1) - HeightDeviationSteps;
                dy = step / (float)HeightDeviationSteps * _shapeSettings.HeightDeviation;
            }
            float posY = (node.Id == 0) ? _baselineY : (_baselineY + dy);

            // Advance cursor for next platform
            _cursorX += platformWidth + _shapeSettings.GapBetweenPlatforms;
            _baselineY = posY;

            return new Vector2(posX, posY);
        }

        private IPlatformContent CreateContent(PlatformContentType contentType, StoryPlatformData storyData, int nodeId)
        {
            switch (contentType)
            {
                case PlatformContentType.Enemy:
                    return CreateEnemyContent(storyData);

                // NPC platforms are produced by the streaming planner as prebuilt NpcContent
                // (with a minted actor + story), never created here from content types.

                case PlatformContentType.Loot:
                    return CreateLootContent(nodeId);

                case PlatformContentType.Quest:
                    return new QuestContent();

                default:
                    return null;
            }
        }

        private LootContent CreateLootContent(int nodeId)
        {
            var context = new LootRollContext(_theme, $"platform:{nodeId}");
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

            // Configure PlatformView with config
            platformView.SetConfig(
                _config.platformMaterial,
                _shapeSettings.PlatformThickness,
                _shapeSettings.RimDropHeight,
                _shapeSettings.CellInset,
                _config.colorVariation ? GetPlatformColor(platform.Id) : null
            );

            platformView.Initialize(platform);

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
