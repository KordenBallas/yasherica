namespace GameInput.Core
{
    /// <summary>
    /// The device families the active-source classifier distinguishes. The infrastructure tracker maps
    /// concrete Unity <c>InputDevice</c>s onto these so the classification rules stay pure C#.
    /// </summary>
    public enum InputDeviceKind
    {
        Keyboard,
        Mouse,
        Gamepad,
        Joystick,
        Touchscreen,
        Other
    }
}
