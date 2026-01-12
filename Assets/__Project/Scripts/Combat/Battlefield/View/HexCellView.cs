using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// View component for hexagonal cells.
    /// Uses LineRenderer to display cell outline.
    /// Subscribes to state change events for reactive updates.
    /// NO business logic - pure visualization.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class HexCellView : MonoBehaviour
    {
        private IHexCell cell;
        private LineRenderer lineRenderer;
        private float size = 1f;
        private MaterialPropertyBlock materialPropertyBlock;
        
        private static readonly Vector3[] basePoints = new Vector3[]
        {
            new Vector3( 1f, 0f, 0f ),
            new Vector3( 0.5f, 0f, 0.866f ),
            new Vector3( -0.5f, 0f, 0.866f ),
            new Vector3( -1f, 0f, 0f ),
            new Vector3( -0.5f, 0f, -0.866f ),
            new Vector3( 0.5f, 0f, -0.866f ),
            new Vector3( 1f, 0f, 0f )   // Close the loop
        };
        
        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = basePoints.Length;

            // Initialize MaterialPropertyBlock for per-instance color changes
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        public void Initialize(IHexCell cell, float hexSize)
        {
            // Unsubscribe from old cell if exists
            if (this.cell != null)
            {
                this.cell.OnStateChanged -= OnCellStateChanged;
            }

            this.cell = cell;
            this.size = hexSize;

            // Subscribe to state changes
            cell.OnStateChanged += OnCellStateChanged;

            UpdateHex();

            // Apply initial state
            if (cell.StateMachine?.CurrentState != null)
            {
                OnCellStateChanged(cell.StateMachine.CurrentState);
            }
        }

        /// <summary>
        /// Event handler called when the cell's state changes.
        /// Updates the visual representation based on the new state.
        /// </summary>
        private void OnCellStateChanged(IHexCellState newState)
        {
            if (newState == null) return;

            // Update color based on new state
            Color newColor = newState.GetColor();
            SetColor(newColor);

            // Update active state
            gameObject.SetActive(cell.IsActive);

            Debug.Log($"[HexCellView] Cell {cell.Coordinates} state changed, applying color {newColor}");
        }
        
        public void UpdateHex()
        {
            if (lineRenderer == null) return;
            
            for (int i = 0; i < basePoints.Length; i++)
            {
                lineRenderer.SetPosition(i, basePoints[i] * size);
            }
        }
        
        public void SetColor(Color color)
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            
            // Set vertex colors (for shaders that support them)
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            
            // Set material property for URP shaders
            if (materialPropertyBlock != null && lineRenderer != null)
            {
                lineRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor("_BaseColor", color);
                materialPropertyBlock.SetColor("_Color", color); // Fallback for some shaders
                lineRenderer.SetPropertyBlock(materialPropertyBlock);
            }
        }
        
        public void SetSize(float newSize)
        {
            size = newSize;
            UpdateHex();
        }

        void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (cell != null)
            {
                cell.OnStateChanged -= OnCellStateChanged;
            }
        }
    }
}

