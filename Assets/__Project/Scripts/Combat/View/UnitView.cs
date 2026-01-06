using Combat.Core;
using UnityEngine;
using TMPro;

namespace Combat.View
{
    /// <summary>
    /// MonoBehaviour view for displaying a unit on the battlefield.
    /// Minimal visualization: colored cube + HP text.
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private TextMeshPro _hpText;
        [SerializeField] private Color _playerColor = Color.blue;
        [SerializeField] private Color _enemyColor = Color.red;
        [SerializeField] private Color _selectedColor = Color.yellow;
        
        private IUnit _unit;
        private Material _material;
        private Color _baseColor;
        private bool _isSelected;
        
        public IUnit Unit => _unit;
        
        private void Awake()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
            
            _material = _meshRenderer.material;
        }
        
        /// <summary>
        /// Initializes the view with a unit.
        /// </summary>
        public void Initialize(IUnit unit, bool isPlayerUnit)
        {
            _unit = unit;
            _baseColor = isPlayerUnit ? _playerColor : _enemyColor;
            UpdateVisuals();
        }
        
        /// <summary>
        /// Updates visuals to match unit state.
        /// </summary>
        public void UpdateVisuals()
        {
            if (_unit == null)
                return;
            
            // Update color
            _material.color = _isSelected ? _selectedColor : _baseColor;
            
            // Update HP text
            if (_hpText != null)
            {
                _hpText.text = $"{_unit.CurrentHP}/{_unit.MaxHP}";
                
                // Color code HP
                float hpPercent = (float)_unit.CurrentHP / _unit.MaxHP;
                if (hpPercent > 0.5f)
                    _hpText.color = Color.green;
                else if (hpPercent > 0.25f)
                    _hpText.color = Color.yellow;
                else
                    _hpText.color = Color.red;
            }
            
            // Hide if dead
            gameObject.SetActive(_unit.IsAlive);
        }
        
        /// <summary>
        /// Sets whether this unit is selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }
    }
}

