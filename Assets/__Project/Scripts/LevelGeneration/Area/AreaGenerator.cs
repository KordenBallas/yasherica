using System.Collections.Generic;
using System.Linq;
using Narrative.Generation;
using Platform;
using UnityEngine;
using Zenject;

namespace LevelGeneration
{
    /// <summary>
    /// Generates area with platforms from graph data.
    /// Uses unified Platform.Factory - state behavior is content-driven.
    /// </summary>
    public class AreaGenerator : IAreaGenerator
    {
        private readonly PlatformGraphData _graph;
        private readonly PerlinNoiseMap _noiseMap;
        private readonly AreaGeneratorConfig _config;
        private readonly Platform.Platform.Factory _platformFactory;
        private readonly LevelNarrative _levelNarrative;

        private readonly Dictionary<int, IPlatform> _platforms = new();
        private readonly Dictionary<int, PlatformView> _platformViews = new();
        private IPlatform _entryPlatform;
        private GameObject _areaGameObject;
        private float _cursorX = 0f;
        private float _baselineY = 0f;

        public IPlatform EntryPlatform => _entryPlatform;

        public AreaGenerator(
            PlatformGraphData graph,
            PerlinNoiseMap noiseMap,
            Platform.Platform.Factory platformFactory,
            LevelNarrative levelNarrative,
            AreaGeneratorConfig config = null)
        {
            _graph = graph;
            _noiseMap = noiseMap;
            _platformFactory = platformFactory;
            _levelNarrative = levelNarrative;
            _config = config ?? new AreaGeneratorConfig();
        }

        public void Generate()
        {
            Clear();

            // Reset cursor for positioning
            _cursorX = 0f;
            _baselineY = 0f;

            // Create Area GameObject as parent
            CreateAreaGameObject();

            // Create all platform instances from graph
            CreatePlatformsFromGraph();

            // Connect platforms based on graph edges
            ConnectPlatforms();

            // Find and set entry platform
            FindEntryPlatform();

            // Create GameObjects for all platforms (all active)
            CreatePlatformGameObjects();

            Debug.Log($"[AreaGenerator] Created {_platformViews.Count} platform GameObjects");

            if (_entryPlatform == null)
            {
                Debug.LogWarning("[AreaGenerator] No entry platform found!");
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

        private void CreatePlatformsFromGraph()
        {
            foreach (var node in _graph.Nodes)
            {
                IPlatform platform = CreatePlatformFromNode(node);
                if (platform != null)
                {
                    _platforms[node.Id] = platform;
                }
            }
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

            // Add content (determines which states activate)
            foreach (var contentType in node.ContentTypes)
            {
                var content = CreateContent(contentType, node.StoryData);
                if (content != null)
                {
                    platform.AddContent(content);
                }
            }

            // Create visual with position from noise map
            var visual = new PlatformVisual();
            Vector2 position2D = CalculatePlatformPosition(node);
            visual.Position = _noiseMap.GetPositionWithHeight(position2D);
            visual.Size = CalculatePlatformSize(node);
            visual.TopBoundary = GeneratePlatformBoundary(visual.Size);

            // Initialize with visual (will also initialize content and state machine)
            platform.Initialize(visual);

            return platform;
        }

        private Vector2 CalculatePlatformPosition(GraphNode node)
        {
            // Calculate position based on previous platforms and gap
            float sx = CalculatePlatformSize(node).x;
            float posX = _cursorX + sx * 0.5f;

            // Height deviation relative to previous node
            float dy = 0f;
            if (node.Id > 0)
            {
                dy = Random.Range(-_config.heightDeviation, _config.heightDeviation);
            }
            float posY = (node.Id == 0) ? _baselineY : (_baselineY + dy);

            // Advance cursor for next platform
            _cursorX += sx + _config.gapBetweenPlatforms;
            _baselineY = posY;

            return new Vector2(posX, posY);
        }

        private Vector2 CalculatePlatformSize(GraphNode node)
        {
            // Random size in range
            float sx = Random.Range(_config.platformSizeMin.x, _config.platformSizeMax.x);
            float sz = Random.Range(_config.platformSizeMin.y, _config.platformSizeMax.y);
            return new Vector2(sx, sz);
        }

        private List<Vector3> GeneratePlatformBoundary(Vector2 size)
        {
            // Use PlatformMeshBuilder to generate proper boundary with jitter
            // This will be updated when mesh is built, but we need initial boundary
            var boundary = new List<Vector3>();
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;

            // Simple rectangular boundary for initial placement
            // The actual boundary will be generated by PlatformMeshBuilder
            boundary.Add(new Vector3(-halfX, 0, -halfZ));
            boundary.Add(new Vector3(halfX, 0, -halfZ));
            boundary.Add(new Vector3(halfX, 0, halfZ));
            boundary.Add(new Vector3(-halfX, 0, halfZ));

            return boundary;
        }

        private IPlatformContent CreateContent(PlatformContentType contentType, StoryPlatformData storyData)
        {
            switch (contentType)
            {
                case PlatformContentType.Enemy:
                    return CreateEnemyContent(storyData);

                case PlatformContentType.Npc:
                    return CreateNpcContent(storyData);

                case PlatformContentType.Loot:
                    return new LootContent();

                case PlatformContentType.Quest:
                    return new QuestContent();

                default:
                    return null;
            }
        }

        private NpcContent CreateNpcContent(StoryPlatformData storyData)
        {
            if (storyData == null || string.IsNullOrEmpty(storyData.NpcId))
            {
                Debug.LogWarning("[AreaGenerator] NPC platform has no NpcId in StoryData - skipping NpcContent creation");
                return null;
            }

            // Find matching NpcAssignment from level narrative
            NpcAssignment assignment = FindAssignmentForNpc(storyData.NpcId);

            if (assignment == null)
            {
                Debug.LogWarning($"[AreaGenerator] No NpcAssignment found for NPC '{storyData.NpcId}' - skipping NpcContent creation");
                return null;
            }

            var npcContent = new NpcContent(assignment);
            Debug.Log($"[AreaGenerator] Created NpcContent for '{assignment.Npc.DisplayName}' (ID: {storyData.NpcId})");
            return npcContent;
        }

        private NpcAssignment FindAssignmentForNpc(string npcId)
        {
            if (_levelNarrative?.Assignments == null)
                return null;

            for (int i = 0; i < _levelNarrative.Assignments.Count; i++)
            {
                if (_levelNarrative.Assignments[i].Npc.NpcId == npcId)
                    return _levelNarrative.Assignments[i];
            }
            return null;
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
                    Debug.LogWarning($"[AreaGenerator] Invalid EnemyId format: '{storyData.EnemyId}'");
                }
            }

            return enemyContent;
        }

        private void ConnectPlatforms()
        {
            foreach (var edge in _graph.Edges)
            {
                if (_platforms.TryGetValue(edge.FromNodeId, out var fromPlatform) &&
                    _platforms.TryGetValue(edge.ToNodeId, out var toPlatform))
                {
                    fromPlatform.AddNeighbor(toPlatform);
                    toPlatform.AddNeighbor(fromPlatform);
                }
            }
        }

        private void FindEntryPlatform()
        {
            int entryNodeId = _graph.EntryNodeId;
            foreach (var platform in _platforms.Values)
            {
                if (platform.Id == entryNodeId)
                {
                    _entryPlatform = platform;
                    Debug.Log("Found entry platform : " + _entryPlatform.Id);
                    return;
                }
            }
            _entryPlatform = _platforms.Values.First();
            Debug.Log("Found entry platform (fallback logic) : " + _entryPlatform.Id);
        }

        private void CreatePlatformGameObjects()
        {
            foreach (var platform in _platforms.Values)
            {
                CreatePlatformGameObject(platform);
            }
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
                _config.platformThickness,
                _config.edgeVertexCount,
                _config.edgeJitter,
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
