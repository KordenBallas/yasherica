namespace Narrative.Stories.Core
{
    /// <summary>Run-lifecycle of one story beat as the planner/relay sees it (FR9). Either status
    /// removes the story from future window plans — a beat is never re-placed as fresh.</summary>
    public enum StoryRunStatus
    {
        /// <summary>Selected into a window plan; the player may not have engaged it yet.</summary>
        Placed = 0,

        /// <summary>Its encounter ran to an outcome (including walking away).</summary>
        Resolved = 1
    }
}
