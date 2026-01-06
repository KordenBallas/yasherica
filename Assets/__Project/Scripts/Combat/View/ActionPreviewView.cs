using Combat.Core;
using UnityEngine;
using System.Collections.Generic;
using Combat.Battlefield;

namespace Combat.View
{
    /// <summary>
    /// Shows preview of valid positions for actions (movement, ability range).
    /// Minimal visualization: simple mesh highlights.
    /// </summary>
    public class ActionPreviewView : MonoBehaviour
    {
        [SerializeField] private GameObject _highlightPrefab;
        [SerializeField] private Transform _highlightsContainer;
        [SerializeField] private Color _moveColor = new Color(0, 1, 0, 0.3f);
        [SerializeField] private Color _attackColor = new Color(1, 0, 0, 0.3f);
        [SerializeField] private float _hexSize = 1.0f;
        
        private List<GameObject> _activeHighlights = new List<GameObject>();
        
        /// <summary>
        /// Shows valid movement positions.
        /// </summary>
        public void ShowMovementRange(List<HexCoordinates> validPositions)
        {
            ClearHighlights();
            
            foreach (var pos in validPositions)
            {
                CreateHighlight(pos, _moveColor);
            }
        }
        
        /// <summary>
        /// Shows valid ability target positions.
        /// </summary>
        public void ShowAbilityRange(List<HexCoordinates> validPositions)
        {
            ClearHighlights();
            
            foreach (var pos in validPositions)
            {
                CreateHighlight(pos, _attackColor);
            }
        }
        
        /// <summary>
        /// Clears all highlights.
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var highlight in _activeHighlights)
            {
                Destroy(highlight);
            }
            _activeHighlights.Clear();
        }
        
        private void CreateHighlight(HexCoordinates hexPos, Color color)
        {
            GameObject highlight;
            
            if (_highlightPrefab != null)
            {
                highlight = Instantiate(_highlightPrefab, _highlightsContainer);
            }
            else
            {
                // Create simple plane if no prefab provided
                highlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
                highlight.transform.SetParent(_highlightsContainer);
                highlight.transform.localScale = new Vector3(_hexSize * 0.1f, 1, _hexSize * 0.1f);
            }
            
            Vector3 worldPos = HexToWorld(hexPos);
            worldPos.y = 0.01f; // Slightly above ground
            highlight.transform.position = worldPos;
            
            var renderer = highlight.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = renderer.material;
                material.color = color;
                
                // Make transparent
                material.SetFloat("_Mode", 3); // Transparent mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            
            _activeHighlights.Add(highlight);
        }
        
        private Vector3 HexToWorld(HexCoordinates hex)
        {
            // Flat-top hex layout
            float x = _hexSize * (3f/2f * hex.Q);
            float z = _hexSize * (Mathf.Sqrt(3f)/2f * hex.Q + Mathf.Sqrt(3f) * hex.R);
            
            return new Vector3(x, 0, z);
        }
    }
}

