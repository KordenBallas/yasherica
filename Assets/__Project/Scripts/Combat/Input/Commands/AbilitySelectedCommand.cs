namespace Combat.Input.Commands
{
    /// <summary>
    /// Command representing an ability selection via keyboard.
    /// </summary>
    public struct AbilitySelectedCommand
    {
        public InputCommandType Type => InputCommandType.AbilitySelected;

        /// <summary>
        /// Index of the selected ability (0-based, maps to Q/W/E/R/T/Y = 0/1/2/3/4/5).
        /// </summary>
        public int AbilityIndex { get; }

        public AbilitySelectedCommand(int abilityIndex)
        {
            AbilityIndex = abilityIndex;
        }
    }
}
