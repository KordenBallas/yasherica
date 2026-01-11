using Combat.Config;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Combat.Input
{
    /// <summary>
    /// PC input controller using mouse and keyboard.
    /// Implements platform-agnostic IInputController interface.
    /// </summary>
    public class PCInputController : MonoBehaviour, IInputController
    {
        [Inject] private InputConfig _config;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _characterTransform;
        
        private bool _isEnabled;
        private float _lastLogTime;
        private const float LOG_INTERVAL = 0.5f; // Log every 0.5 seconds to avoid spam
        
        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }
        
        public bool IsMovementModeActive
        {
            get
            {
                bool isActive = _isEnabled && IsKeyPressed(_config.movementModeKey);
                
                // Throttled logging to avoid spam
                if (Time.time - _lastLogTime > LOG_INTERVAL)
                {
                    if (isActive || IsKeyPressed(_config.movementModeKey))
                    {
                        Debug.Log($"[PCInputController] IsMovementModeActive: {isActive} (Enabled: {_isEnabled}, Key: {IsKeyPressed(_config.movementModeKey)})");
                        _lastLogTime = Time.time;
                    }
                }
                
                return isActive;
            }
        }
        
        public Vector3? GetMovementDirection()
        {
            if (!IsMovementModeActive)
            {
                return null;
            }
            
            if (_characterTransform == null)
            {
                Debug.LogWarning("[PCInputController] GetMovementDirection: _characterTransform is null");
                return null;
            }
            
            if (Mouse.current == null)
            {
                Debug.LogWarning("[PCInputController] Mouse input device not available");
                return null;
            }
            
            // Raycast from mouse to battlefield plane
            Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, _characterTransform.position);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Vector3 direction = (worldPoint - _characterTransform.position);
                direction.y = 0; // Flatten to XZ plane
                
                bool hasDirection = direction.magnitude > _config.inputDeadzone;
                
                if (hasDirection)
                {
                    direction.Normalize();
                    Debug.Log($"[PCInputController] Direction calculated: {direction} (magnitude: {direction.magnitude})");
                    return direction;
                }
                else
                {
                    Debug.Log($"[PCInputController] Direction too small (magnitude: {direction.magnitude}, deadzone: {_config.inputDeadzone})");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning("[PCInputController] Raycast did not hit ground plane");
            }
            
            return null;
        }
        
        public bool IsConfirmPressed => 
            _isEnabled && IsKeyPressedThisFrame(_config.confirmKey);
        
        public bool IsCancelPressed => 
            _isEnabled && IsKeyPressedThisFrame(_config.cancelKey);
        
        public void Enable()
        {
            _isEnabled = true;
            Debug.Log($"[PCInputController] Enabled (Movement: {_config.movementModeKey}, Confirm: {_config.confirmKey})");
        }
        
        public void Disable()
        {
            _isEnabled = false;
            Debug.Log("[PCInputController] Disabled");
        }
        
        /// <summary>
        /// Sets the character transform for direction calculation.
        /// Called by initializer when character is set up.
        /// </summary>
        public void SetCharacterTransform(Transform characterTransform)
        {
            _characterTransform = characterTransform;
            Debug.Log($"[PCInputController] SetCharacterTransform called with {characterTransform?.name ?? "null"}");
        }
        
        /// <summary>
        /// Checks if a key is currently pressed using the new Input System.
        /// </summary>
        private bool IsKeyPressed(KeyCode keyCode)
        {
            // Handle mouse buttons specially
            if (keyCode == KeyCode.Mouse0)
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (keyCode == KeyCode.Mouse1)
                return Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (keyCode == KeyCode.Mouse2)
                return Mouse.current != null && Mouse.current.middleButton.isPressed;
            
            // Handle keyboard keys
            if (Keyboard.current == null)
                return false;
            
            Key key = ConvertKeyCodeToKey(keyCode);
            if (key == Key.None)
                return false;
            
            return Keyboard.current[key].isPressed;
        }
        
        /// <summary>
        /// Checks if a key was pressed this frame using the new Input System.
        /// </summary>
        private bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
            // Handle mouse buttons specially
            if (keyCode == KeyCode.Mouse0)
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (keyCode == KeyCode.Mouse1)
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            if (keyCode == KeyCode.Mouse2)
                return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
            
            // Handle keyboard keys
            if (Keyboard.current == null)
                return false;
            
            Key key = ConvertKeyCodeToKey(keyCode);
            if (key == Key.None)
                return false;
            
            return Keyboard.current[key].wasPressedThisFrame;
        }
        
        /// <summary>
        /// Converts Unity's old KeyCode to the new Input System Key enum.
        /// </summary>
        private Key ConvertKeyCodeToKey(KeyCode keyCode)
        {
            // Map common keys - extend as needed
            switch (keyCode)
            {
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;
                case KeyCode.Alpha3: return Key.Digit3;
                case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.Alpha5: return Key.Digit5;
                case KeyCode.Alpha6: return Key.Digit6;
                case KeyCode.Alpha7: return Key.Digit7;
                case KeyCode.Alpha8: return Key.Digit8;
                case KeyCode.Alpha9: return Key.Digit9;
                default:
                    Debug.LogWarning($"[PCInputController] KeyCode {keyCode} not mapped to new Input System Key");
                    return Key.None;
            }
        }
    }
}
