namespace Combat.Input.Commands
{
    /// <summary>
    /// Command representing a request to enter change direction mode.
    /// </summary>
    public struct ChangeDirectionModeCommand
    {
        public InputCommandType Type => InputCommandType.ChangeDirection;

        public ChangeDirectionModeCommand(bool _)
        {
            // Empty struct needs constructor to be valid
        }
    }
}
