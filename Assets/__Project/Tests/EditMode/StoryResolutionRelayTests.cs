using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Core;
using Narrative.Stories.Core;
using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class StoryResolutionRelayTests
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
        private StoryRunLedger _storyLedger;
        private ThreadLedger _threadLedger;
        private StoryResolutionRelay _relay;

        private static readonly FactKeyShapeCore PassClearedShape =
            new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool);

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false))
            });
            _store = new FactStore(registry, logger);
            _fake = new FakeStoryManager();
            _runner = new DialogueRunner(new DialogueSession(_fake), _store,
                new FactEffectApplier(new SubjectResolver(logger), logger),
                new DialogueTagParser(registry, logger), recorder: null, logger: logger);
            _storyLedger = new StoryRunLedger();
            _threadLedger = new ThreadLedger();
            _relay = new StoryResolutionRelay(_runner, _storyLedger, _threadLedger);
            _relay.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _relay.Dispose();
        }

        private static Casting Cast(string storyId, string threadId, string enemyId = null)
        {
            var actor = new NpcInstance("npc_07", "arch_road_bandit", "Razor", "free_blades");
            var ctx = new ContextBag().BindSubject("$self", "npc_07");
            return new Casting(actor, new DialogueData("dlg_toll", "{}", "start",
                System.Array.Empty<string>(), new[] { PassClearedShape }, new[] { "shakedown" }),
                null, enemyId, ctx, storyId, threadId);
        }

        [Test]
        public void EngagedOutcome_ResolvesStory_AndAdvancesThread()
        {
            _fake.Script(FakeStoryManager.Frame.Line("A single line."));
            _threadLedger.Open("barn_raid", ThreadKind.Ephemeral, 0);

            _runner.Begin(Cast("story_barn_victim", "barn_raid"));
            _runner.Continue(); // past the line to the natural end ("exit")

            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.IsTrue(_storyLedger.TryGet("story_barn_victim", out var entry));
            Assert.AreEqual(StoryRunStatus.Resolved, entry.Status);
            Assert.IsTrue(_threadLedger.TryGet("barn_raid", out var record));
            Assert.AreEqual(1, record.Stage);
            Assert.IsTrue(record.AdvancedSinceLastTick);
        }

        [Test]
        public void LeaveOutcome_ResolvesStory_ButDoesNotAdvanceThread()
        {
            _fake.Script(FakeStoryManager.Frame.Line("A single line."));
            _threadLedger.Open("barn_raid", ThreadKind.Ephemeral, 0);

            _runner.Begin(Cast("story_barn_victim", "barn_raid"));
            _runner.Leave(); // walk away from the gated line

            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.IsTrue(_storyLedger.IsPlacedOrResolved("story_barn_victim")); // never re-offered
            Assert.IsTrue(_threadLedger.TryGet("barn_raid", out var record));
            Assert.AreEqual(0, record.Stage); // but no thread progress: the errand can still lapse
        }

        [Test]
        public void CastingWithoutStory_RecordsNothing()
        {
            _fake.Script(FakeStoryManager.Frame.Line("A single line."));

            _runner.Begin(Cast(storyId: "", threadId: ""));
            _runner.Continue();

            Assert.AreEqual(0, _storyLedger.Entries.Count);
            Assert.AreEqual(0, _threadLedger.Threads.Count);
        }

        [Test]
        public void PostCombatResume_AdvancesThreadExactlyOnce()
        {
            // The combat suspension resumes and ends through the same OnDialogueEnded gate: one
            // encounter, one advance.
            _fake.Script(
                FakeStoryManager.Frame.Line("Steel rings.", "start-combat: bandit"),
                FakeStoryManager.Frame.Line("You stand over them."));
            _threadLedger.Open("barn_raid", ThreadKind.Ephemeral, 0);

            _runner.Begin(Cast("story_barn_raid", "barn_raid", enemyId: "enemy_brute"));
            Assert.AreEqual(DialogueRunnerState.AwaitingExternal, _runner.State);

            _runner.ReportCombatResult(true);
            _runner.Continue(); // past the post-combat line to the end

            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.IsTrue(_threadLedger.TryGet("barn_raid", out var record));
            Assert.AreEqual(1, record.Stage);
        }
    }
}
