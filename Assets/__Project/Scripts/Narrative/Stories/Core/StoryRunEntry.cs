namespace Narrative.Stories.Core
{
    /// <summary>One story's run-lifecycle entry in the <see cref="IStoryRunLedger"/>.</summary>
    public sealed class StoryRunEntry
    {
        public string StoryId { get; }
        public string ThreadId { get; }
        public StoryRunStatus Status { get; internal set; }
        public int WindowPlaced { get; }

        public StoryRunEntry(string storyId, string threadId, StoryRunStatus status, int windowPlaced)
        {
            StoryId = storyId ?? string.Empty;
            ThreadId = threadId ?? string.Empty;
            Status = status;
            WindowPlaced = windowPlaced;
        }
    }
}
