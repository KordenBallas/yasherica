using System.Collections.Generic;

namespace Narrative.Stories.Core
{
    /// <summary>
    /// Run-scoped record of which story beats were placed/resolved (FR9 — no stale re-placement).
    /// The planner's old per-window <c>usedStoryIds</c> promoted to run scope: the look-ahead plans
    /// against the *current* story state, so a beat already offered, resolved, or on a failed thread
    /// is never re-placed as fresh across a window boundary. Entry order is planner-driven and
    /// replay-stable. Ambient-colour chatter deliberately bypasses this ledger (it may repeat).
    /// </summary>
    public interface IStoryRunLedger
    {
        IReadOnlyList<StoryRunEntry> Entries { get; }

        /// <summary>Records a placement; idempotent on story id.</summary>
        void NotePlaced(string storyId, string threadId, int windowIndex);

        /// <summary>Marks a story's encounter as run to an outcome. Upserts: the legacy
        /// per-encounter path resolves stories the streaming planner never placed.</summary>
        void NoteResolved(string storyId);

        bool IsPlacedOrResolved(string storyId);

        bool TryGet(string storyId, out StoryRunEntry entry);

        /// <summary>Replaces the ledger's content from a snapshot (save/load restore, FR12).</summary>
        void Restore(IReadOnlyList<StoryRunEntry> entries);
    }
}
