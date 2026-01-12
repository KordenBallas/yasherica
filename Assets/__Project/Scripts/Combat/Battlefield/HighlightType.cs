namespace Combat.Battlefield
{
    /// <summary>
    /// Defines the type of cell highlight for visual feedback.
    /// </summary>
    public enum HighlightType
    {
        Hovered,      // Currently hovering over cell
        Selected,     // User selected cell
        ValidMove,    // Legal movement target
        InvalidMove,  // Illegal movement target
        EnemyThreat   // Enemy threat range
    }
}
