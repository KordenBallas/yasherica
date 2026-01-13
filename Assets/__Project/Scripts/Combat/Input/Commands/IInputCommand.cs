namespace Combat.Input.Commands
{
    /// <summary>
    /// Base interface for all input commands.
    /// Separate from IAction (game actions) - these represent raw user input events.
    /// Input commands are transient and do not modify game state directly.
    /// </summary>
    public interface IInputCommand
    {
        /// <summary>
        /// Timestamp when the command was created.
        /// Useful for debugging and input replay.
        /// </summary>
        float Timestamp { get; }

        /// <summary>
        /// The type of input command for dispatch.
        /// </summary>
        InputCommandType Type { get; }
    }
}
