namespace Combat.Input.Commands
{
    /// <summary>
    /// Command emitted when the player aborts an in-progress volley aim (right-click while holding the
    /// execute key): releasing the key then does NOT fire the queue (D2).
    /// </summary>
    public readonly struct VolleyAimCancelledCommand : IInputCommand
    {
        public float Timestamp { get; }
        public InputCommandType Type => InputCommandType.VolleyAimCancelled;

        public VolleyAimCancelledCommand(float timestamp)
        {
            Timestamp = timestamp;
        }
    }
}
