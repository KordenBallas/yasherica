using Combat.Config;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Combat.Input
{
    /// <summary>
    /// Joystick/gamepad input controller.
    /// Uses Unity's new Input System for gamepad support.
    /// </summary>
    public class JoystickInputController : MonoBehaviour, IInputController
    {
        [Inject] private InputConfig _config;
        
        private bool _isEnabled;
        
        public bool IsMovementModeActive => _isEnabled;
        
        public Vector3? GetMovementDirection()
        {
            if (!_isEnabled) return null;
            
            if (Gamepad.current == null)
                return null;
            
            Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
            float h = leftStick.x;
            float v = leftStick.y;
            
            if (Mathf.Abs(h) < _config.inputDeadzone && Mathf.Abs(v) < _config.inputDeadzone)
                return null;
            
            return new Vector3(h, 0, v).normalized;
        }
        
        public bool IsConfirmPressed => 
            _isEnabled && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        
        public bool IsCancelPressed => 
            _isEnabled && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        
        public void Enable()
        {
            _isEnabled = true;
            Debug.Log("[JoystickInputController] Enabled");
        }
        
        public void Disable()
        {
            _isEnabled = false;
            Debug.Log("[JoystickInputController] Disabled");
        }
    }
}
