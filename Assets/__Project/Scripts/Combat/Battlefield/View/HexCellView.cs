using UnityEngine;

namespace Combat.Battlefield
{
    [RequireComponent(typeof(LineRenderer))]
    public class HexCellView : MonoBehaviour
    {
        private IHexCell cell;
        private LineRenderer lineRenderer;
        private float size = 1f;
        private Color lastColor;
        
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
        }
        
        public void Initialize(IHexCell cell, float hexSize)
        {
            this.cell = cell;
            this.size = hexSize;
            this.lastColor = cell.Color; // Initialize cache with current color
            UpdateHex();
            SetColor(cell.Color); // Apply initial color
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
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        
        public void SetSize(float newSize)
        {
            size = newSize;
            UpdateHex();
        }
        
        void Update()
        {
            if (cell != null)
            {
                // Only update LineRenderer if color actually changed
                if (cell.Color != lastColor)
                {
                    Debug.Log("[HexCellView] Changing color from " + lastColor + " to " + cell.Color);
                    SetColor(cell.Color);
                    lastColor = cell.Color;
                }
                //Debug.Log("[HexCellView] Last color is " + lastColor + ", current one is " + cell.Color);
                gameObject.SetActive(cell.IsActive);
            }
            /*else
            {
                Debug.Log("[HexCellView] No hex cell model is linked to this view");
            }*/
        }
    }
}

