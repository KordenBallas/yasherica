namespace Combat.Input.Commands
{
    /// <summary>
    /// Command emitted when the player begins aiming the queued volley (holds the execute key): the
    /// hero turns toward the cursor while held and the queue fires on release (D2).
    /// </summary>
    public readonly struct VolleyAimStartedCommand : IInputCommand
    {
        public float Timestamp { get; }
        public InputCommandType Type => InputCommandType.VolleyAimStarted;

        public VolleyAimStartedCommand(float timestamp)
        {
            Timestamp = timestamp;
        }
    }
}
