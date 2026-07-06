using UnityEngine;

namespace Combat.Config
{
    /// <summary>
    /// Configuration for platform-specific input settings.
    /// </summary>
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Combat/Input Config")]
    public class InputConfig : ScriptableObject
    {
        [Header("Platform Detection")]
        [Tooltip("Target runtime platform for this config")]
        public RuntimePlatform targetPlatform;
        
        [Tooltip("Type of input controller to use")]
        public InputControllerType controllerType;
        
        [Header("PC Settings")]
        [Tooltip("Hold this key to enter movement mode; release to confirm the move")]
        public KeyCode movementModeKey = KeyCode.M;

        [Tooltip("Key for change direction mode")]
        public KeyCode changeDirectionKey = KeyCode.S;

        [Tooltip("Key for confirming actions")]
        public KeyCode confirmKey = KeyCode.Mouse0;

        [Tooltip("Key for canceling actions")]
        public KeyCode cancelKey = KeyCode.Mouse1;

        [Tooltip("Key for executing the ability queue")]
        public KeyCode executeQueueKey = KeyCode.Return;

        [Tooltip("Holding the execute key at least this long enters volley aim mode; a shorter " +
                 "tap fires the queue immediately along the current facing (D7)")]
        public float volleyAimHoldThresholdSeconds = 0.25f;

        [Header("Ability Hotkeys")]
        [Tooltip("Ability hotkeys (Q/W/E/R/T/Y by default, index 0-5)")]
        public System.Collections.Generic.List<KeyCode> abilityKeys = new System.Collections.Generic.List<KeyCode>
        {
            KeyCode.Q,
            KeyCode.W,
            KeyCode.E,
            KeyCode.R,
            KeyCode.T,
            KeyCode.Y
        };
        
        [Header("Mobile Settings")]
        [Tooltip("Touch drag sensitivity multiplier")]
        public float touchDragSensitivity = 1.0f;
        
        [Tooltip("Radius for tap confirmation detection")]
        public float tapConfirmRadius = 50f;
        
        [Header("Joystick Settings")]
        [Tooltip("Horizontal axis name for joystick")]
        public string horizontalAxisName = "Horizontal";
        
        [Tooltip("Vertical axis name for joystick")]
        public string verticalAxisName = "Vertical";
        
        [Tooltip("Confirm button name for joystick")]
        public string confirmButtonName = "Submit";
        
        [Tooltip("Input deadzone for joystick")]
        public float inputDeadzone = 0.1f;
    }
    
    public enum InputControllerType
    {
        PC,
        Mobile,
        Joystick
    }
}
