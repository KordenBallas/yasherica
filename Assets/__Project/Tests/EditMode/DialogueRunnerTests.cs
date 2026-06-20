using System.Collections.Generic;
using Core.Logging;
using Narrative;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DialogueRunnerTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(string message) { }
            public void Warning(string message) => Warnings.Add(message);
            public void Error(string message) { }
        }

        private FakeLogger _logger;
        private FactStore _store;
        private FakeStoryManager _fake;
        private DialogueRunner _runner;

        private static readonly FactKeyShapeCore PassClearedShape =
            new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool);

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false))
            });
            _store = new FactStore(registry, _logger);
            var resolver = new SubjectResolver(_logger);
            var applier = new FactEffectApplier(resolver, _logger);
            var parser = new DialogueTagParser(registry, _logger);
            _fake = new FakeStoryManager();
            var session = new DialogueSession(_fake);
            _runner = new DialogueRunner(session, _store, applier, parser, recorder: null, logger: _logger);
        }

        private static DialogueData Dialogue() => new DialogueData(
            "dlg_toll", "{}", "start", System.Array.Empty<string>(),
            new[] { PassClearedShape }, new[] { "shakedown" });

        private static Casting Cast(string enemyId, QuestData quest)
        {
            var actor = new NpcInstance("npc_07", "arch_road_bandit", "Razor", "free_blades");
            var ctx = new ContextBag().BindSubject("$self", "npc_07");
            return new Casting(actor, Dialogue(), quest, enemyId, ctx);
        }

        private static StoryChoice[] Choices() => new[]
        {
            new StoryChoice(0, "Pay the toll."),
            new StoryChoice(1, "Draw steel.")
        };

        [Test]
        public void PayBranch_AppliesFact_AndEnds()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("Pay the toll, or bleed.", "speaker: Razor"),
                FakeStoryManager.Frame.ChoicePoint(Choices(), new[]
                {
                    new[] { FakeStoryManager.Frame.Line("The road's yours.", "fact: world.pass_cleared Set true") },
                    new[] { FakeStoryManager.Frame.Line("Steel rings.", "start-combat: bandit") }
                }));

            string speaker = null;
            string ended = null;
            _runner.OnSpeakerChanged += s => speaker = s;
            _runner.OnDialogueEnded += o => ended = o;

            _runner.Begin(Cast("enemy_brute", null));
            Assert.AreEqual("Razor", speaker);

            _runner.SelectChoice(0); // Pay
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.AreEqual("exit", ended);
        }

        [Test]
        public void CombatBranch_SuspendsUntilResult_ThenResumes()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("Pay the toll, or bleed."),
                FakeStoryManager.Frame.ChoicePoint(Choices(), new[]
                {
                    new[] { FakeStoryManager.Frame.Line("paid", "fact: world.pass_cleared Set true") },
                    new[]
                    {
                        FakeStoryManager.Frame.Line("Steel rings.", "start-combat: bandit"),
                        FakeStoryManager.Frame.Line("You stand over them.", "fact: world.pass_cleared Set true")
                    }
                }));

            string combatEnemy = null;
            _runner.OnCombatTriggered += e => combatEnemy = e;

            _runner.Begin(Cast("enemy_brute", null));
            _runner.SelectChoice(1); // Draw steel -> start-combat

            // B1: suspended, no further Ink consumed, pass_cleared not yet written.
            Assert.AreEqual(DialogueRunnerState.AwaitingExternal, _runner.State);
            Assert.AreEqual("enemy_brute", combatEnemy);
            Assert.IsFalse(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());

            _runner.ReportCombatResult(true);
            Assert.AreEqual(true, _fake.GetVariable("combat_won"));
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
        }

        [Test]
        public void DoubleResume_IsGuarded()
        {
            _fake.Script(FakeStoryManager.Frame.Line("hi"));
            _runner.Begin(Cast("enemy_brute", null));

            // Not awaiting -> ReportCombatResult is a no-op + warns.
            int before = _logger.Warnings.Count;
            _runner.ReportCombatResult(true);
            Assert.AreEqual(before + 1, _logger.Warnings.Count);
        }

        [Test]
        public void EmptyCombatSlot_FailsClosed_NoTransition_SetsSafeWriteBack()
        {
            _fake.Script(FakeStoryManager.Frame.Line("draws", "start-combat: bandit"));

            bool combatRaised = false;
            _runner.OnCombatTriggered += _ => combatRaised = true;

            _runner.Begin(Cast(enemyId: null, quest: null)); // combat slot unfilled
            Assert.IsFalse(combatRaised);
            Assert.AreEqual(false, _fake.GetVariable("combat_won"));
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State); // continued past, then ended
        }

        [Test]
        public void OfferQuest_FilledSlot_StartsQuest_AndSetsAcceptedVar()
        {
            var quest = new QuestData("qst_clear_pass", "", "", System.Array.Empty<QuestObjective>(),
                new[] { "errand" }, System.Array.Empty<FactEffectCore>(), System.Array.Empty<FactEffectCore>());
            _fake.Script(FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"));

            string startedQuest = null;
            _runner.OnQuestStarted += q => startedQuest = q;

            _runner.Begin(Cast(enemyId: null, quest: quest));
            Assert.AreEqual("qst_clear_pass", startedQuest);
            Assert.AreEqual(true, _fake.GetVariable("quest_accepted"));
            Assert.IsNotNull(_runner.ActiveQuest);
            Assert.AreEqual(QuestState.Active, _runner.ActiveQuest.State);
        }

        [Test]
        public void OfferQuest_EmptySlot_FailsClosed()
        {
            _fake.Script(FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"));
            _runner.Begin(Cast(enemyId: null, quest: null));
            Assert.AreEqual(false, _fake.GetVariable("quest_accepted"));
            Assert.IsNull(_runner.ActiveQuest);
        }
    }
}
