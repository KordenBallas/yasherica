using System.Collections.Generic;

namespace Narrative.Stories.Core
{
    /// <summary>Default <see cref="IStoryRunLedger"/>: a pure-C# list in first-seen order.</summary>
    public sealed class StoryRunLedger : IStoryRunLedger
    {
        private const int UnplannedWindow = -1;

        private readonly List<StoryRunEntry> _entries = new List<StoryRunEntry>();

        public IReadOnlyList<StoryRunEntry> Entries => _entries;

        public void NotePlaced(string storyId, string threadId, int windowIndex)
        {
            if (string.IsNullOrEmpty(storyId) || TryGet(storyId, out _))
            {
                return;
            }

            _entries.Add(new StoryRunEntry(storyId, threadId, StoryRunStatus.Placed, windowIndex));
        }

        public void NoteResolved(string storyId)
        {
            if (string.IsNullOrEmpty(storyId))
            {
                return;
            }

            if (TryGet(storyId, out var entry))
            {
                entry.Status = StoryRunStatus.Resolved;
                return;
            }

            _entries.Add(new StoryRunEntry(storyId, string.Empty, StoryRunStatus.Resolved, UnplannedWindow));
        }

        public bool IsPlacedOrResolved(string storyId)
        {
            return TryGet(storyId, out _);
        }

        public void Restore(IReadOnlyList<StoryRunEntry> entries)
        {
            _entries.Clear();
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                {
                    _entries.Add(entries[i]);
                }
            }
        }

        public bool TryGet(string storyId, out StoryRunEntry entry)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].StoryId, storyId, System.StringComparison.Ordinal))
                {
                    entry = _entries[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }
}
