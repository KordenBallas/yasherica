namespace Combat.Input.Commands
{
    /// <summary>
    /// Command emitted when movement mode is activated or deactivated.
    /// </summary>
    public readonly struct MovementModeChangedCommand : IInputCommand
    {
        public float Timestamp { get; }
        public InputCommandType Type => InputCommandType.MovementModeChanged;

        /// <summary>
        /// True if movement mode became active, false if deactivated.
        /// </summary>
        public bool IsActive { get; }

        public MovementModeChangedCommand(bool isActive, float timestamp)
        {
            IsActive = isActive;
            Timestamp = timestamp;
        }
    }
}
