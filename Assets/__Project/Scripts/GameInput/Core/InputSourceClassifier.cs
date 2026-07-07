namespace GameInput.Core
{
    /// <summary>
    /// Pure classification rules for active-source detection (Input Foundation R5): which
    /// <see cref="InputSource"/> a device actuation counts as, and whether the actuation is strong
    /// enough to count at all. Two rules matter beyond the obvious mapping: weak actuations are ignored
    /// so gamepad stick drift and idle noise never steal the active source, and a NON-native gamepad is
    /// classified as Touch — Unity's on-screen controls (the touch overlay stick/button) drive a virtual
    /// gamepad, and without this rule touching the overlay would flip every prompt to controller cues.
    /// </summary>
    public sealed class InputSourceClassifier
    {
        /// <summary>Minimum control actuation that counts as "the player used this device". Buttons
        /// actuate at 1; a drifting stick sits well below this.</summary>
        public const float MinActuation = 0.3f;

        /// <summary>The source this actuation makes active, or null when it must be ignored
        /// (too weak, or a device family we do not track).</summary>
        public InputSource? Classify(InputDeviceKind kind, bool isNativeDevice, float actuationMagnitude)
        {
            if (actuationMagnitude < MinActuation)
            {
                return null;
            }

            switch (kind)
            {
                case InputDeviceKind.Keyboard:
                case InputDeviceKind.Mouse:
                    return InputSource.KeyboardMouse;
                case InputDeviceKind.Gamepad:
                case InputDeviceKind.Joystick:
                    return isNativeDevice ? InputSource.Gamepad : InputSource.Touch;
                case InputDeviceKind.Touchscreen:
                    return InputSource.Touch;
                default:
                    return null;
            }
        }
    }
}
