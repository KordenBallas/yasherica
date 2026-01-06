using UnityEngine;

namespace Combat.Battlefield
{
    [RequireComponent(typeof(LineRenderer))]
    public class HexCellView : MonoBehaviour
    {
        private IHexCell cell;
        private LineRenderer lineRenderer;
        private float size = 1f;
        
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
            UpdateHex();
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
                SetColor(cell.Color);
                gameObject.SetActive(cell.IsActive);
            }
        }
    }
}

