using Combat.Config;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Zenject;

namespace Combat.Input
{
    /// <summary>
    /// Mobile input controller using touch input.
    /// Uses Unity's new Input System for touch support.
    /// </summary>
    public class MobileInputController : MonoBehaviour, IInputController
    {
        [Inject] private InputConfig _config;
        
        private Vector3? _dragDirection;
        private bool _isEnabled;
        
        private void Awake()
        {
            // Enable enhanced touch support for the new Input System
            EnhancedTouchSupport.Enable();
        }
        
        public bool IsMovementModeActive => _isEnabled && Touchscreen.current != null && Touchscreen.current.touches.Count > 0;
        
        public Vector3? GetMovementDirection()
        {
            if (!IsMovementModeActive) return null;
            
            // TODO: Implement touch drag direction detection
            // For now, return null (stub)
            return _dragDirection;
        }
        
        public bool IsConfirmPressed
        {
            get
            {
                if (!_isEnabled || Touchscreen.current == null)
                    return false;
                
                var touches = Touchscreen.current.touches;
                if (touches.Count == 0)
                    return false;
                
                var touch = touches[0];
                return touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended;
            }
        }
        
        public bool IsCancelPressed => false; // TODO: Implement
        
        public void Enable()
        {
            _isEnabled = true;
            Debug.Log("[MobileInputController] Enabled");
        }
        
        public void Disable()
        {
            _isEnabled = false;
            Debug.Log("[MobileInputController] Disabled");
        }
        
        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
        }
    }
}
