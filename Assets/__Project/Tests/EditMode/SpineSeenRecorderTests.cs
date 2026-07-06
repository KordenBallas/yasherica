using System;
using System.Collections.Generic;
using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Core;
using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The cross-run cursor's single writer (D20, P3-3): when a spine beat's dialogue ends - with
    /// any outcome, walk-away included - <c>world.&lt;storyId&gt;.spine_seen</c> is set; non-spine
    /// and story-less encounters write nothing.
    /// </summary>
    [TestFixture]
    public class SpineSeenRecorderTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private FactStore _store;
        private FakeStoryManager _fake;
        private DialogueRunner _runner;
        private SpineSeenRecorder _recorder;

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "spine_seen", FactScope.PerStory, FactValueType.Bool,
                    FactValue.FromBool(false), FactHorizon.Meta)
            });
            _store = new FactStore(registry, logger);
            _fake = new FakeStoryManager();
            _runner = new DialogueRunner(new DialogueSession(_fake), _store,
                new FactEffectApplier(new SubjectResolver(logger), logger),
                new DialogueTagParser(registry, logger), recorder: null, logger: logger);
            _recorder = new SpineSeenRecorder(_runner, new[] { SpineStory("story_spine_hint"), OrdinaryStory("story_barn_victim") }, _store);
            _recorder.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _recorder.Dispose();
        }

        private static StoryTemplateData SpineStory(string id)
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            return new StoryTemplateData(id, slots, Array.Empty<FactPredicate>(), null,
                new[] { "spine" }, "", true, weight: 10);
        }

        private static StoryTemplateData OrdinaryStory(string id)
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            return new StoryTemplateData(id, slots, Array.Empty<FactPredicate>(), null,
                new[] { "villager" }, "", false, weight: 10);
        }

        private static Casting Cast(string storyId)
        {
            var actor = new NpcInstance("npc_07", "arch_villager", "Mara", "village");
            var ctx = new ContextBag().BindSubject("$self", "npc_07");
            return new Casting(actor, new DialogueData("dlg_x", "{}", "start",
                Array.Empty<string>(), Array.Empty<FactKeyShapeCore>(), new[] { "talk" }),
                null, null, ctx, storyId, "");
        }

        private bool Seen(string storyId) => _store.GetBool(WorldFacts.SpineSeen, storyId);

        [Test]
        public void EngagedEnd_OnASpineStory_WritesTheSeenFact()
        {
            _fake.Script(FakeStoryManager.Frame.Line("A single line."));

            _runner.Begin(Cast("story_spine_hint"));
            Assert.IsFalse(Seen("story_spine_hint"), "Seen means delivered, not begun.");
            _runner.Continue(); // to the natural end

            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.IsTrue(Seen("story_spine_hint"));
        }

        [Test]
        public void LeaveOutcome_StillCountsAsSeen()
        {
            // The player entered and the reveal lines played; walking away does not un-see them.
            _fake.Script(FakeStoryManager.Frame.Line("A single line."));

            _runner.Begin(Cast("story_spine_hint"));
            _runner.Leave();

            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.IsTrue(Seen("story_spine_hint"));
        }

        [Test]
        public void NonSpineStory_WritesNothing()
        {
            _fake.Script(FakeStoryManager.Frame.Line("A single line."));

            _runner.Begin(Cast("story_barn_victim"));
            _runner.Continue();

            Assert.IsFalse(Seen("story_barn_victim"));
            Assert.AreEqual(0, _store.Snapshot().Count, "Ordinary encounters leave no cursor entries.");
        }

        [Test]
        public void StorylessCasting_WritesNothing()
        {
            _fake.Script(FakeStoryManager.Frame.Line("A single line."));

            _runner.Begin(Cast(storyId: ""));
            _runner.Continue();

            Assert.AreEqual(0, _store.Snapshot().Count);
        }
    }
}
