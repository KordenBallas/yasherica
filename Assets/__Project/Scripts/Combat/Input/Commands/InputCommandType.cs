namespace Combat.Input.Commands
{
    /// <summary>
    /// Enumeration of input command types.
    /// </summary>
    public enum InputCommandType
    {
        MovementModeChanged,
        MovementDirectionChanged,
        MovementConfirmed,
        MovementCancelled,
        AbilitySelected,
        AbilityCancelled,
        AbilityConfirmed,
        ExecuteQueue,
        ChangeDirection,
        VolleyAimStarted,
        VolleyAimCancelled
    }
}
