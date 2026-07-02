using System.Collections.Generic;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Spawns GameObjects for platform content (enemies, NPCs, loot) when platforms are entered.
    /// Manages the lifecycle of spawned content objects.
    /// </summary>
    public class ContentSpawner : MonoBehaviour
    {
        [Header("Content Prefabs")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject npcPrefab;
        [SerializeField] private GameObject lootPrefab;
        [SerializeField] private GameObject questPrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnHeightOffset = 0.5f;
        [SerializeField] private bool spawnOnPlatformEnter = true;
        
        private readonly Dictionary<IPlatform, List<GameObject>> spawnedContent = new();
        [Inject] private IGameLogger _logger;
        
        void OnEnable()
        {
            Core.Events.PlatformEvents.OnPlatformEntered += OnPlatformEntered;
            Core.Events.PlatformEvents.OnPlatformExited += OnPlatformExited;
        }
        
        void OnDisable()
        {
            Core.Events.PlatformEvents.OnPlatformEntered -= OnPlatformEntered;
            Core.Events.PlatformEvents.OnPlatformExited -= OnPlatformExited;
        }
        
        void OnPlatformEntered(IPlatform platform)
        {
            if (platform == null || !spawnOnPlatformEnter) return;
            
            SpawnContentForPlatform(platform);
        }
        
        void OnPlatformExited(IPlatform platform)
        {
            if (platform == null) return;
            
            // Optionally despawn content when leaving platform
            // For now, we keep it spawned
        }
        
        public void SpawnContentForPlatform(IPlatform platform)
        {
            if (platform == null || platform.Visual == null) return;
            
            // Don't spawn if already spawned
            if (spawnedContent.ContainsKey(platform))
            {
                return;
            }
            
            var contentList = new List<GameObject>();
            
            foreach (var content in platform.Contents)
            {
                GameObject prefab = GetPrefabForContent(content);
                if (prefab == null) continue;
                
                Vector3 spawnPosition = GetSpawnPosition(platform, content);
                GameObject spawned = Instantiate(prefab, spawnPosition, Quaternion.identity);
                
                // Parent to platform if possible
                var platformView = GetPlatformView(platform);
                if (platformView != null)
                {
                    spawned.transform.SetParent(platformView.transform);
                }
                
                contentList.Add(spawned);
                
                _logger?.Info(LogCategory.Platform, $"[ContentSpawner] Spawned {content.Type} for platform {platform.Id} at {spawnPosition}");
            }
            
            if (contentList.Count > 0)
            {
                spawnedContent[platform] = contentList;
            }
        }
        
        public void DespawnContentForPlatform(IPlatform platform)
        {
            if (platform == null || !spawnedContent.TryGetValue(platform, out var contentList))
            {
                return;
            }
            
            foreach (var content in contentList)
            {
                if (content != null)
                {
                    Destroy(content);
                }
            }
            
            spawnedContent.Remove(platform);
        }
        
        GameObject GetPrefabForContent(IPlatformContent content)
        {
            return content.Type switch
            {
                ContentType.Enemy => enemyPrefab,
                ContentType.Npc => npcPrefab,
                // Loot pickups are spawned by PlatformLootSpawnCoordinator with
                // per-item visuals; the generic prefab path would double-spawn.
                ContentType.Loot => null,
                ContentType.Quest => questPrefab,
                _ => null
            };
        }
        
        Vector3 GetSpawnPosition(IPlatform platform, IPlatformContent content)
        {
            Vector3 basePosition = platform.Visual.Position;

            // For platforms with active combat, we might want to spawn on hex cells
            var combatController = platform.GetCombatController();
            if (combatController?.Battlefield != null)
            {
                // Spawn at a random hex cell in the battlefield
                var cellsInBoundary = combatController.Battlefield.GetCellsInBoundary();
                if (cellsInBoundary.Count > 0)
                {
                    int randomIndex = Random.Range(0, cellsInBoundary.Count);
                    var coords = cellsInBoundary[randomIndex];
                    basePosition = combatController.Battlefield.HexToWorld(coords);
                }
            }

            // Add height offset
            basePosition.y += spawnHeightOffset;

            // Add some random variation
            basePosition.x += Random.Range(-0.5f, 0.5f);
            basePosition.z += Random.Range(-0.5f, 0.5f);

            return basePosition;
        }
        
        PlatformView GetPlatformView(IPlatform platform)
        {
            // Use PlatformRegistry to get the view
            // We'll need to add a method to PlatformRegistry to get view from platform
            // For now, search by GameObject name
            var platformGO = GameObject.Find($"Platform_{platform.Id}");
            if (platformGO != null)
            {
                return platformGO.GetComponent<PlatformView>();
            }

            // Alternative: search in scene
            var allViews = FindObjectsByType<PlatformView>(FindObjectsSortMode.None);
            foreach (var view in allViews)
            {
                // We can't directly access the platform from view, so we'll use GameObject name
                if (view.gameObject.name == $"Platform_{platform.Id}")
                {
                    return view;
                }
            }
            
            return null;
        }
        
        void OnDestroy()
        {
            // Clean up all spawned content
            foreach (var contentList in spawnedContent.Values)
            {
                foreach (var content in contentList)
                {
                    if (content != null)
                    {
                        Destroy(content);
                    }
                }
            }
            spawnedContent.Clear();
        }
    }
}

