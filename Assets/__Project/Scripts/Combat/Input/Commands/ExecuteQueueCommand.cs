namespace Combat.Input.Commands
{
    /// <summary>
    /// Command representing a request to execute the ability queue.
    /// </summary>
    public struct ExecuteQueueCommand
    {
        public InputCommandType Type => InputCommandType.ExecuteQueue;

        public ExecuteQueueCommand(bool _)
        {
            // Empty struct needs constructor to be valid
        }
    }
}
