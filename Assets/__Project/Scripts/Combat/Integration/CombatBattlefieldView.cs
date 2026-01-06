using Combat.Controller;
using Combat.Core;
using UnityEngine;
using Combat.Battlefield;

namespace Combat.Integration
{
    /// <summary>
    /// Visual adapter for displaying combat on the battlefield.
    /// Integrates CombatBattlefield with BattlefieldView.
    /// </summary>
    public class CombatBattlefieldView : MonoBehaviour
    {
        [SerializeField] private BattlefieldView _battlefieldView;
        
        private CombatBattlefield _combatBattlefield;
        private ICombatController _gameController;
        
        public void Initialize(CombatBattlefield combatBattlefield, ICombatController gameController)
        {
            _combatBattlefield = combatBattlefield;
            _gameController = gameController;
            
            // Subscribe to state changes
            _gameController.OnStateChanged += OnStateChanged;
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
            // Refresh battlefield visualization if needed
            UpdateBattlefieldHighlights(newState);
        }
        
        private void UpdateBattlefieldHighlights(ICombatState gameState)
        {
            // This can be extended to show special highlights on the battlefield
            // For example, highlighting cells where units are positioned
        }
        
        /// <summary>
        /// Highlights valid movement cells on the battlefield.
        /// </summary>
        public void HighlightMovementRange(IUnit unit, int range)
        {
            var validPositions = _combatBattlefield.GetValidMovementPositions(unit, range, _gameController.CombatState);
            
            // Use battlefield view to highlight these positions
            // This would integrate with your existing BattlefieldView highlighting system
        }
        
        /// <summary>
        /// Highlights valid ability target cells.
        /// </summary>
        public void HighlightAbilityRange(HexCoordinates casterPosition, int range)
        {
            var validPositions = _combatBattlefield.GetValidAbilityTargetPositions(casterPosition, range);
            
            // Use battlefield view to highlight these positions
        }
        
        /// <summary>
        /// Clears all highlights from the battlefield.
        /// </summary>
        public void ClearHighlights()
        {
            // Clear battlefield highlights
        }
    }
}

