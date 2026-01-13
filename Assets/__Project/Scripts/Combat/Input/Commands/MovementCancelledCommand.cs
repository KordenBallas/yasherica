namespace Combat.Input.Commands
{
    /// <summary>
    /// Command emitted when the player cancels the current movement.
    /// </summary>
    public readonly struct MovementCancelledCommand : IInputCommand
    {
        public float Timestamp { get; }
        public InputCommandType Type => InputCommandType.MovementCancelled;

        public MovementCancelledCommand(float timestamp)
        {
            Timestamp = timestamp;
        }
    }
}
