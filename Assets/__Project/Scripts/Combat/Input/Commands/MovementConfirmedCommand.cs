namespace Combat.Input.Commands
{
    /// <summary>
    /// Command emitted when the player confirms the current movement.
    /// </summary>
    public readonly struct MovementConfirmedCommand : IInputCommand
    {
        public float Timestamp { get; }
        public InputCommandType Type => InputCommandType.MovementConfirmed;

        public MovementConfirmedCommand(float timestamp)
        {
            Timestamp = timestamp;
        }
    }
}
