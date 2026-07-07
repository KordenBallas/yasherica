namespace GameInput.Core
{
    /// <summary>
    /// An explicitly acknowledged coverage gap (Input Foundation R2/R4): a (action × source) pair the
    /// catalog deliberately leaves unbound because closing it is interaction design owned by a separate
    /// brief, not plumbing. Every gap names its reason and the ROADMAP item that tracks the brief, so
    /// the coverage test can fail on any gap that is neither bound nor consciously deferred — and on
    /// any allowlist entry that has silently become bound (a stale allowlist is a bug).
    /// </summary>
    public sealed class DeferredBindingGap
    {
        public GameAction Action { get; }
        public InputSource Source { get; }
        public string Reason { get; }
        public string RoadmapReference { get; }

        public DeferredBindingGap(GameAction action, InputSource source, string reason, string roadmapReference)
        {
            Action = action;
            Source = source;
            Reason = reason ?? string.Empty;
            RoadmapReference = roadmapReference ?? string.Empty;
        }
    }
}
