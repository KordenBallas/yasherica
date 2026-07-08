namespace MetaProgression.Core
{
    /// <summary>
    /// Per-token gating mark (meta-progression FR2/FR15). <see cref="Base"/> tokens are available
    /// from run 1; <see cref="MetaGated"/> tokens are absent from every draw (dig, world loot, quest
    /// rewards, recipes, mutation variants) until their deed is met. An unmarked token defaults to
    /// <see cref="Base"/> so all existing content keeps working without edits.
    /// </summary>
    public enum GatingMark
    {
        Base = 0,
        MetaGated = 1
    }
}
