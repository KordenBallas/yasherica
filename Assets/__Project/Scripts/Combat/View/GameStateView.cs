using Combat.Controller;
using Combat.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Combat.View
{
    /// <summary>
    /// MonoBehaviour view that synchronizes with the game state.
    /// Creates and updates UnitViews.
    /// </summary>
    public class CombatStateView : MonoBehaviour
    {
        [SerializeField] private GameObject _unitViewPrefab;
        [SerializeField] private Transform _unitsContainer;
        [SerializeField] private float _hexSize = 1.0f;
        
        private ICombatController _gameController;
        private Dictionary<int, UnitView> _unitViews = new Dictionary<int, UnitView>();
        private int _localPlayerId;
        
        public void Initialize(ICombatController gameController, int localPlayerId)
        {
            _gameController = gameController;
            _localPlayerId = localPlayerId;
            
            // Subscribe to state changes
            _gameController.OnStateChanged += OnStateChanged;
            
            // Create initial views
            UpdateViews(_gameController.CombatState);
        }
        
        private void OnDestroy()
        {
            if (_gameController != null)
            {
                _gameController.OnStateChanged -= OnStateChanged;
            }
        }
        
        private void OnStateChanged(ICombatState newState)
        {
            UpdateViews(newState);
        }
        
        private void UpdateViews(ICombatState gameState)
        {
            // Update existing views or create new ones
            foreach (var unit in gameState.Units)
            {
                if (!_unitViews.TryGetValue(unit.Id, out var view))
                {
                    // Create new view
                    view = CreateUnitView(unit);
                    _unitViews[unit.Id] = view;
                }
                
                // Update position
                Vector3 worldPos = HexToWorld(unit.Position);
                view.transform.position = worldPos;
                
                // Update visuals
                view.UpdateVisuals();
            }
            
            // Remove views for dead units
            var deadUnitIds = _unitViews.Keys.Where(id =>
            {
                var unit = gameState.GetUnit(id);
                return unit == null || !unit.IsAlive;
            }).ToList();
            
            foreach (var id in deadUnitIds)
            {
                if (_unitViews.TryGetValue(id, out var view))
                {
                    Destroy(view.gameObject);
                    _unitViews.Remove(id);
                }
            }
        }
        
        private UnitView CreateUnitView(IUnit unit)
        {
            GameObject viewObj = Instantiate(_unitViewPrefab, _unitsContainer);
            UnitView view = viewObj.GetComponent<UnitView>();
            
            if (view == null)
            {
                view = viewObj.AddComponent<UnitView>();
            }
            
            bool isPlayerUnit = unit.Owner.Id == _localPlayerId;
            view.Initialize(unit, isPlayerUnit);
            
            return view;
        }
        
        /// <summary>
        /// Converts hex coordinates to world position.
        /// </summary>
        private Vector3 HexToWorld(Battlefield.HexCoordinates hex)
        {
            // Flat-top hex layout
            float x = _hexSize * (3f/2f * hex.Q);
            float z = _hexSize * (Mathf.Sqrt(3f)/2f * hex.Q + Mathf.Sqrt(3f) * hex.R);
            
            return new Vector3(x, 0, z);
        }
    }
}

