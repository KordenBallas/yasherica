using System.Collections.Generic;
using UnityEngine;
using System; // For Type.GetType

namespace Combat.Battlefield
{
    /// <summary>
    /// MonoBehaviour that renders the hex grid battlefield on a combat platform.
    /// Creates visual hex cells based on the battlefield's grid.
    /// </summary>
    public class BattlefieldView : MonoBehaviour
    {
        [Header("Hex Cell Prefab")]
        [SerializeField] private GameObject hexCellPrefab;
        
        [Header("Visual Settings")]
        [SerializeField] private Color defaultCellColor = Color.black;
        [SerializeField] private float cellHeightOffset = 0.1f; // Offset above platform surface
        
        private IBattlefield battlefield;
        private readonly List<GameObject> spawnedCells = new();
        private bool isActive = true; // Enabled by default
        
        private const string DEFAULT_HEX_CELL_PREFAB_PATH = "Prefabs/HexagonOutline";
        
        /// <summary>
        /// Gets the hex cell prefab, loading from Resources if not set.
        /// </summary>
        private GameObject GetHexCellPrefab()
        {
            if (hexCellPrefab != null)
            {
                return hexCellPrefab;
            }
            
            // Try to load from Resources as fallback
            hexCellPrefab = Resources.Load<GameObject>(DEFAULT_HEX_CELL_PREFAB_PATH);
            
            if (hexCellPrefab == null)
            {
                Debug.LogError($"[BattlefieldView] Could not load hex cell prefab from Resources path: {DEFAULT_HEX_CELL_PREFAB_PATH}");
            }
            
            return hexCellPrefab;
        }
        
        /// <summary>
        /// Sets the hex cell prefab programmatically.
        /// </summary>
        public void SetHexCellPrefab(GameObject prefab)
        {
            hexCellPrefab = prefab;
        }
        
        public void Initialize(IBattlefield battlefield)
        {
            this.battlefield = battlefield;
            Regenerate();
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            
            foreach (var cell in spawnedCells)
            {
                if (cell != null)
                {
                    cell.SetActive(active);
                }
            }
        }
        
        public void Regenerate()
        {
            Clear();
            
            if (battlefield == null)
            {
                Debug.LogWarning("[BattlefieldView] Cannot regenerate: battlefield is null.");
                return;
            }
            
            // Activate battlefield if not already active (needed for GetCellsInBoundary to work)
            if (!battlefield.IsActive)
            {
                battlefield.Activate();
            }
            
            var prefab = GetHexCellPrefab();
            if (prefab == null)
            {
                Debug.LogError("[BattlefieldView] Missing hexCellPrefab! Could not load from Resources either.");
                return;
            }
            
            // Get all cells in boundary
            var cellsInBoundary = battlefield.GetCellsInBoundary();
            Debug.Log("[BattlefieldView] Battlefield cells count is " + cellsInBoundary.Count);
            
            // Get hex size from battlefield
            float hexSize = battlefield.HexSize;
            Vector3 battlefieldCenter = battlefield.Center;
            Debug.Log($"[BattlefieldView] Using hexSize: {hexSize}");
            
            foreach (var coords in cellsInBoundary)
            {
                var cell = battlefield.GetCellAt(coords);
                if (cell == null) continue;
                
                // Get local position (offset from center) from grid
                Vector3 localPos;
                if (battlefield.Grid is HexGridBase gridBase)
                {
                    localPos = gridBase.GetCellPosition(coords);
                }
                else
                {
                    // Fallback: convert world to local
                    Vector3 worldPos = battlefield.HexToWorld(coords);
                    localPos = worldPos - battlefieldCenter;
                }
                
                // Set Y to height offset
                localPos.y = cellHeightOffset;
                
                // Spawn visual cell
                var hexGO = Instantiate(prefab, transform);
                hexGO.transform.localPosition = localPos;
                
                // Try HexCellView first (new component)
                var hexCellView = hexGO.GetComponent<HexCellView>();
                if (hexCellView != null)
                {
                    cell.Color = defaultCellColor; // Set color on model (single source of truth)
                    hexCellView.Initialize(cell, hexSize);
                    Debug.Log($"[BattlefieldView] Initialized HexCellView with size: {hexSize}");
                }
                else
                {
                    // Fallback to legacy HexagonController
                    var hexController = hexGO.GetComponent<HexagonController>();
                    if (hexController != null)
                    {
                        hexController.SetSize(hexSize);
                        hexController.SetColor(defaultCellColor);
                        // UpdateHex is now called by SetSize, but calling it again won't hurt
                        hexController.UpdateHex();
                        Debug.Log($"[BattlefieldView] Initialized HexagonController with size: {hexSize}");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattlefieldView] Hex cell prefab '{prefab.name}' has neither HexCellView nor HexagonController component!");
                    }
                }
                
                spawnedCells.Add(hexGO);
                hexGO.SetActive(isActive);
            }
        }
        
        public void Clear()
        {
            foreach (var cell in spawnedCells)
            {
                if (cell != null)
                {
                    DestroyImmediate(cell);
                }
            }
            spawnedCells.Clear();
        }
        
        void OnDestroy()
        {
            Clear();
        }
    }
}

