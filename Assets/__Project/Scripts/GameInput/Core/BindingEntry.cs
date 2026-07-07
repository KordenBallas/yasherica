namespace GameInput.Core
{
    /// <summary>
    /// One row of the binding catalog: how a named action is reachable on one source, plus the short
    /// text cue prompts display for it ("F", "RT", "Tap"). <see cref="ControlPath"/> is the Input System
    /// control path the runtime asset must carry; it is empty for touch rows satisfied by the on-screen
    /// overlay or by direct pointer taps rather than by a device binding.
    /// </summary>
    public sealed class BindingEntry
    {
        public GameAction Action { get; }
        public InputSource Source { get; }
        public string ControlPath { get; }
        public string Cue { get; }

        public BindingEntry(GameAction action, InputSource source, string controlPath, string cue)
        {
            Action = action;
            Source = source;
            ControlPath = controlPath ?? string.Empty;
            Cue = cue ?? string.Empty;
        }

        /// <summary>True when the row is realized by a device binding in the actions asset (and the
        /// drift-guard test must find <see cref="ControlPath"/> there), false for overlay/pointer rows.</summary>
        public bool HasControlPath => ControlPath.Length > 0;
    }
}
