using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class StoryRunLedgerTests
    {
        private StoryRunLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            _ledger = new StoryRunLedger();
        }

        [Test]
        public void NotePlaced_RecordsEntry_AndBlocksReplacement()
        {
            _ledger.NotePlaced("story_barn_raid", "barn_raid", windowIndex: 1);

            Assert.IsTrue(_ledger.IsPlacedOrResolved("story_barn_raid"));
            Assert.IsTrue(_ledger.TryGet("story_barn_raid", out var entry));
            Assert.AreEqual(StoryRunStatus.Placed, entry.Status);
            Assert.AreEqual("barn_raid", entry.ThreadId);
            Assert.AreEqual(1, entry.WindowPlaced);
        }

        [Test]
        public void NotePlaced_IsIdempotent_KeepsFirstWindow()
        {
            _ledger.NotePlaced("story_a", "t", 0);
            _ledger.NotePlaced("story_a", "other", 3);

            Assert.AreEqual(1, _ledger.Entries.Count);
            Assert.IsTrue(_ledger.TryGet("story_a", out var entry));
            Assert.AreEqual(0, entry.WindowPlaced);
            Assert.AreEqual("t", entry.ThreadId);
        }

        [Test]
        public void NoteResolved_UpgradesPlacedEntry()
        {
            _ledger.NotePlaced("story_a", "t", 0);
            _ledger.NoteResolved("story_a");

            Assert.IsTrue(_ledger.TryGet("story_a", out var entry));
            Assert.AreEqual(StoryRunStatus.Resolved, entry.Status);
            Assert.AreEqual(1, _ledger.Entries.Count);
        }

        [Test]
        public void NoteResolved_UnplannedStory_UpsertsResolvedEntry()
        {
            // The legacy per-encounter path resolves stories the streaming planner never placed.
            _ledger.NoteResolved("legacy_story");

            Assert.IsTrue(_ledger.IsPlacedOrResolved("legacy_story"));
            Assert.IsTrue(_ledger.TryGet("legacy_story", out var entry));
            Assert.AreEqual(StoryRunStatus.Resolved, entry.Status);
        }

        [Test]
        public void UnknownStory_IsNotPlacedOrResolved()
        {
            Assert.IsFalse(_ledger.IsPlacedOrResolved("never_seen"));
        }

        [Test]
        public void EmptyStoryId_IsIgnored()
        {
            _ledger.NotePlaced("", "t", 0);
            _ledger.NoteResolved(null);

            Assert.AreEqual(0, _ledger.Entries.Count);
        }
    }
}
