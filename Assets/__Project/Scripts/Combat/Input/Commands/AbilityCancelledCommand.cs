namespace Combat.Input.Commands
{
    /// <summary>
    /// Command representing ability targeting cancellation.
    /// </summary>
    public struct AbilityCancelledCommand
    {
        public InputCommandType Type => InputCommandType.AbilityCancelled;

        public AbilityCancelledCommand(bool _)
        {
            // Empty struct needs constructor to be valid
        }
    }
}
