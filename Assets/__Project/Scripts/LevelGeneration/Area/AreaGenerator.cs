using System.Collections.Generic;
using System.Linq;
using Character;
using Combat.Controller;
using Combat.Input;
using Combat.Integration;
using Combat.Core;
using Core.Camera;
using Platform;
using UnityEngine;
using Zenject;

namespace LevelGeneration
{
    public class AreaGenerator : IAreaGenerator
    {
        private readonly PlatformGraphData graph;
        private readonly PerlinNoiseMap noiseMap;
        private readonly AreaGeneratorConfig config;
        private readonly IFactory<ICombatController> _controllerFactory;
        private readonly ICameraService _cameraService;
        private readonly CharacterCombatInitializer _characterInitializer;
        private readonly IInputController _inputController;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly ICharacterRegistry _characterRegistry;
        private readonly Dictionary<int, IPlatform> platforms = new();
        private readonly Dictionary<int, PlatformView> platformViews = new();
        private IPlatform entryPlatform;
        private GameObject areaGameObject;
        private float cursorX = 0f;
        private float baselineY = 0f;
        
        public IPlatform EntryPlatform => entryPlatform;
        
        public AreaGenerator(
            PlatformGraphData graph, 
            PerlinNoiseMap noiseMap, 
            IFactory<ICombatController> controllerFactory,
            ICameraService cameraService,
            CharacterCombatInitializer characterInitializer,
            IInputController inputController,
            IPlayerRegistry playerRegistry,
            ICharacterRegistry characterRegistry,
            AreaGeneratorConfig config = null)
        {
            this.graph = graph;
            this.noiseMap = noiseMap;
            _controllerFactory = controllerFactory;
            _cameraService = cameraService;
            _characterInitializer = characterInitializer;
            _inputController = inputController;
            _playerRegistry = playerRegistry;
            _characterRegistry = characterRegistry;
            this.config = config ?? new AreaGeneratorConfig();
        }
        
        public void Generate()
        {
            Clear();
            
            // Reset cursor for positioning
            cursorX = 0f;
            baselineY = 0f;
            
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
            
            Debug.Log($"[AreaGenerator] Created {platformViews.Count} platform GameObjects");
            
            if (entryPlatform == null)
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
                foreach (var platform in platforms.Values)
                {
                    registry.UnregisterPlatform(platform);
                }
            }
            
            // Destroy all platform GameObjects
            foreach (var view in platformViews.Values)
            {
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
            platformViews.Clear();
            
            // Destroy area GameObject
            if (areaGameObject != null)
            {
                Object.Destroy(areaGameObject);
                areaGameObject = null;
            }
            
            platforms.Clear();
            entryPlatform = null;
        }
        
        private void CreateAreaGameObject()
        {
            areaGameObject = new GameObject("Area");
        }
        
        private void CreatePlatformsFromGraph()
        {
            foreach (var node in graph.Nodes)
            {
                IPlatform platform = CreatePlatformFromNode(node);
                if (platform != null)
                {
                    platforms[node.Id] = platform;
                }
            }
        }
        
        private IPlatform CreatePlatformFromNode(GraphNode node)
        {
            // Create platform based on type
            IPlatform platform = node.Type == PlatformType.Combat 
                ? new CombatPlatform(node.Id, _controllerFactory, _cameraService, _characterInitializer, _inputController, _playerRegistry, _characterRegistry)
                : new SimplePlatform(node.Id);
            
            // Add content BEFORE Initialize
            foreach (var contentType in node.ContentTypes)
            {
                var content = CreateContent(contentType);
                if (content != null)
                {
                    platform.AddContent(content);
                }
            }
            
            // Create visual with position from noise map
            var visual = new PlatformVisual();
            Vector2 position2D = CalculatePlatformPosition(node);
            visual.Position = noiseMap.GetPositionWithHeight(position2D);
            visual.Size = CalculatePlatformSize(node);
            visual.TopBoundary = GeneratePlatformBoundary(visual.Size);
            
            // Initialize with visual (will also initialize content)
            platform.Initialize(visual);
            
            return platform;
        }
        
        private Vector2 CalculatePlatformPosition(GraphNode node)
        {
            // Calculate position based on previous platforms and gap
            float sx = CalculatePlatformSize(node).x;
            float posX = cursorX + sx * 0.5f;
            
            // Height deviation relative to previous node
            float dy = 0f;
            if (node.Id > 0)
            {
                dy = Random.Range(-config.heightDeviation, config.heightDeviation);
            }
            float posY = (node.Id == 0) ? baselineY : (baselineY + dy);
            
            // Advance cursor for next platform
            cursorX += sx + config.gapBetweenPlatforms;
            baselineY = posY;
            
            return new Vector2(posX, posY);
        }
        
        private Vector2 CalculatePlatformSize(GraphNode node)
        {
            // Random size in range
            float sx = Random.Range(config.platformSizeMin.x, config.platformSizeMax.x);
            float sz = Random.Range(config.platformSizeMin.y, config.platformSizeMax.y);
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
        
        private IPlatformContent CreateContent(PlatformContentType contentType)
        {
            return contentType switch
            {
                PlatformContentType.Enemy => new EnemyContent(),
                PlatformContentType.Npc => new NpcContent(),
                PlatformContentType.Loot => new LootContent(),
                PlatformContentType.Quest => new QuestContent(),
                _ => null
            };
        }
        
        private void ConnectPlatforms()
        {
            foreach (var edge in graph.Edges)
            {
                if (platforms.TryGetValue(edge.FromNodeId, out var fromPlatform) &&
                    platforms.TryGetValue(edge.ToNodeId, out var toPlatform))
                {
                    fromPlatform.AddNeighbor(toPlatform);
                    toPlatform.AddNeighbor(fromPlatform);
                }
            }
        }
        
        private void FindEntryPlatform()
        {
            int entryNodeId = graph.EntryNodeId;
            foreach (var platform in platforms.Values)
            {
                if (platform.Id == entryNodeId)
                {
                    entryPlatform = platform;
                    Debug.Log("Found entry platform : " + entryPlatform.Id);
                    return;
                }
                
            }
            entryPlatform = platforms.Values.First();
            Debug.Log("Found entry platform (fallback logic) : " + entryPlatform.Id);
            /*
            // Entry platform is the one with no incoming edges
            var nodesWithIncomingEdges = graph.Edges.Select(e => e.ToNodeId).ToHashSet();

            foreach (var node in graph.Nodes)
            {
                if (!nodesWithIncomingEdges.Contains(node.Id))
                {
                    if (platforms.TryGetValue(node.Id, out var platform))
                    {
                        entryPlatform = platform;
                        break;
                    }
                }
            }

            // Fallback: use first platform if no entry found
            if (entryPlatform == null && platforms.Count > 0)
            {
                entryPlatform = platforms.Values.First();
                Debug.Log("Found entry platform (fallback logic): " + entryPlatform);
            }*/
        }
        
        private void CreatePlatformGameObjects()
        {
            foreach (var platform in platforms.Values)
            {
                CreatePlatformGameObject(platform);
            }
        }
        
        private void CreatePlatformGameObject(IPlatform platform)
        {
            if (platform == null || areaGameObject == null) return;
            
            // Create GameObject for platform
            var platformGO = new GameObject($"Platform_{platform.Id}");
            platform.Visual.GameObject = platformGO; // TODO: refactor this
            platformGO.transform.SetParent(areaGameObject.transform);
            platformGO.transform.position = platform.Visual.Position;
            
            // Add PlatformView component
            var platformView = platformGO.AddComponent<PlatformView>();
            
            // Configure PlatformView with config
            platformView.SetConfig(
                config.platformMaterial,
                config.platformThickness,
                config.edgeVertexCount,
                config.edgeJitter,
                config.colorVariation ? GetPlatformColor(platform.Id) : null
            );
            
            platformView.Initialize(platform);
            
            // Store the view
            platformViews[platform.Id] = platformView;
            
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
            if (!config.colorVariation) return null;
            
            int totalPlatforms = platforms.Count;
            float hue = (platformId / (float)Mathf.Max(1, totalPlatforms)) * 0.6f;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }
        
    }
}

