namespace GameInput.Core
{
    /// <summary>
    /// The three supported input sources (Input Foundation R3). "Active source" means the one the
    /// player last actuated; it drives which cue every on-screen prompt shows.
    /// </summary>
    public enum InputSource
    {
        KeyboardMouse,
        Gamepad,
        Touch
    }
}
