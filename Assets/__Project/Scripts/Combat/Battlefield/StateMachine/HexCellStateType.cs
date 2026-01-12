namespace Combat.Battlefield
{
    /// <summary>
    /// Enumeration of all possible cell states.
    /// Used for state queries and transition validation.
    /// </summary>
    public enum HexCellStateType
    {
        /// <summary>
        /// Cell exists but is not visible or active.
        /// </summary>
        Inactive,

        /// <summary>
        /// Default active state with no special status.
        /// </summary>
        Idle,

        /// <summary>
        /// Cell is highlighted with various visual feedback types.
        /// </summary>
        Highlighted,

        /// <summary>
        /// Cell is occupied by a unit.
        /// </summary>
        Occupied,

        /// <summary>
        /// Cell is visible but cannot be interacted with.
        /// </summary>
        Disabled
    }
}
