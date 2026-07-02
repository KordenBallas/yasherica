using System.Collections;
using Combat.Battlefield;
using Combat.Controller;
using Combat.Core;
using Core.Logging;
using UnityEngine;

namespace Combat.Animation
{
    /// <summary>
    /// MonoBehaviour that handles character animation in combat.
    /// Single responsibility: listen to state changes and trigger animations.
    /// Uses strategy pattern for actual animation implementation.
    /// </summary>
    public class CharacterCombatAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _characterTransform;
        
        private ICharacterMovementAnimator _animationStrategy;
        private IBattlefield _battlefield;
        private ICombatController _combatController;
        private int _characterUnitId;
        private bool _isAnimating;
        private IGameLogger _logger;

        public void Initialize(
            ICharacterMovementAnimator animationStrategy,
            IBattlefield battlefield,
            ICombatController combatController,
            int characterUnitId,
            IGameLogger logger)
        {
            _logger = logger;
            _animationStrategy = animationStrategy;
            _battlefield = battlefield;
            _combatController = combatController;
            _characterUnitId = characterUnitId;
            
            if (_characterTransform == null)
                _characterTransform = transform;
            
            _combatController.OnStateChanged += OnCombatStateChanged;
            
            _logger?.Info(LogCategory.Combat,$"[CharacterCombatAnimator] Initialized for unit ID: {_characterUnitId}");
        }
        
        private void OnCombatStateChanged(ICombatState newState)
        {
            if (_isAnimating) return;
            
            var characterUnit = newState.GetUnit(_characterUnitId);
            if (characterUnit == null) return;
            
            // Check if position changed
            Vector3 targetWorldPos = _battlefield.HexToWorld(characterUnit.Position);
            float distance = Vector3.Distance(_characterTransform.position, targetWorldPos);
            
            if (distance > 0.1f)
            {
                _logger?.Info(LogCategory.Combat,$"[CharacterCombatAnimator] Position changed, animating to {characterUnit.Position}");
                StartCoroutine(AnimateToPosition(targetWorldPos));
            }
        }
        
        private IEnumerator AnimateToPosition(Vector3 targetPosition)
        {
            _isAnimating = true;
            Vector3 startPosition = _characterTransform.position;
            
            yield return _animationStrategy.AnimateMovement(
                _characterTransform, 
                startPosition, 
                targetPosition);
            
            _isAnimating = false;
        }
        
        private void OnDestroy()
        {
            if (_combatController != null)
                _combatController.OnStateChanged -= OnCombatStateChanged;
        }
    }
}
