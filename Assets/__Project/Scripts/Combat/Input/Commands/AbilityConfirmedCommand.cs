namespace Combat.Input.Commands
{
    /// <summary>
    /// Command emitted when the player releases an ability key to confirm targeting.
    /// </summary>
    public struct AbilityConfirmedCommand
    {
        public InputCommandType Type => InputCommandType.AbilityConfirmed;
        public float Timestamp { get; }

        public AbilityConfirmedCommand(float timestamp)
        {
            Timestamp = timestamp;
        }
    }
}
