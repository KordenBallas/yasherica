using System.Collections.Generic;
using CharacterProgression.Core;
using Combat.Core;
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
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings.Add(message);
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeRecorder : IRunProgressionRecorder
        {
            public readonly List<string> Started = new();
            public readonly List<string> Completed = new();
            public readonly List<string> Failed = new();
            public void StartQuest(string questId) => Started.Add(questId);
            public void CompleteQuest(string questId) => Completed.Add(questId);
            public void FailQuest(string questId) => Failed.Add(questId);
            public void RecordNpcEncounter(string npcId) { }
            public void RecordChoice(string key, string value) { }
        }

        private FakeLogger _logger;
        private FactStore _store;
        private FactEffectApplier _applier;
        private DialogueTagParser _parser;
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
            _applier = new FactEffectApplier(resolver, _logger);
            _parser = new DialogueTagParser(registry, _logger);
            _fake = new FakeStoryManager();
            var session = new DialogueSession(_fake);
            _runner = new DialogueRunner(session, _store, _applier, _parser, recorder: null, logger: _logger);
        }

        /// <summary>Builds a second runner with a progression recorder (and optional quest registry) over
        /// the same scripted story manager.</summary>
        private DialogueRunner BuildRunnerWith(IRunProgressionRecorder recorder, ILiveQuestRegistry questRegistry = null)
        {
            var session = new DialogueSession(_fake);
            return new DialogueRunner(session, _store, _applier, _parser, recorder, questRegistry, _logger);
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
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State); // gated on the prompt line

            _runner.Continue(); // advance past the prompt to the choices
            _runner.SelectChoice(0); // Pay -> branch line applies the fact, then gates on the line
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State);

            _runner.Continue(); // advance past the branch line to the end
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
            CombatInitiator? initiator = null;
            _runner.OnCombatTriggered += (e, who) => { combatEnemy = e; initiator = who; };

            _runner.Begin(Cast("enemy_brute", null));
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State); // gated on the prompt line
            _runner.Continue(); // advance past the prompt to the choices
            _runner.SelectChoice(1); // Draw steel -> start-combat

            // B1: suspended, no further Ink consumed, pass_cleared not yet written.
            Assert.AreEqual(DialogueRunnerState.AwaitingExternal, _runner.State);
            Assert.AreEqual("enemy_brute", combatEnemy);
            Assert.AreEqual(CombatInitiator.Enemy, initiator); // a start-combat tag = the NPC turned hostile
            Assert.IsFalse(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());

            _runner.ReportCombatResult(true);
            Assert.AreEqual(true, _fake.GetVariable("combat_won"));
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State); // gates on the post-combat line

            _runner.Continue(); // advance past the post-combat line to the end
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
            _runner.OnCombatTriggered += (_, __) => combatRaised = true;

            _runner.Begin(Cast(enemyId: null, quest: null)); // combat slot unfilled
            Assert.IsFalse(combatRaised);
            Assert.AreEqual(false, _fake.GetVariable("combat_won"));
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State); // failed closed, gates on the line

            _runner.Continue();
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

        private static FactEffectCore SetPassCleared() =>
            new FactEffectCore(FactNamespace.World, "", "pass_cleared", FactEffectOp.Set, FactValue.FromBool(true));

        private static QuestData QuestWith(IReadOnlyList<QuestObjective> objectives = null) =>
            new QuestData("qst_clear_pass", "", "", objectives ?? System.Array.Empty<QuestObjective>(),
                new[] { "errand" }, new[] { SetPassCleared() }, new[] { SetPassCleared() });

        [Test]
        public void OfferThenComplete_CompletesQuest_AppliesEffects_Records_Raises()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"),
                FakeStoryManager.Frame.Line("done.", "complete-quest:"));

            var recorder = new FakeRecorder();
            var runner = BuildRunnerWith(recorder);
            string completed = null;
            runner.OnQuestCompleted += q => completed = q;

            runner.Begin(Cast(enemyId: null, quest: QuestWith()));
            runner.Continue(); // advance to the complete-quest line

            Assert.AreEqual(QuestState.Completed, runner.ActiveQuest.State);
            Assert.AreEqual("qst_clear_pass", completed);
            CollectionAssert.Contains(recorder.Completed, "qst_clear_pass");
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
        }

        [Test]
        public void OfferThenFail_FailsQuest_Records_Raises()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"),
                FakeStoryManager.Frame.Line("you blew it.", "fail-quest:"));

            var recorder = new FakeRecorder();
            var runner = BuildRunnerWith(recorder);
            string failed = null;
            runner.OnQuestFailed += q => failed = q;

            runner.Begin(Cast(enemyId: null, quest: QuestWith()));
            runner.Continue(); // advance to the fail-quest line

            Assert.AreEqual(QuestState.Failed, runner.ActiveQuest.State);
            Assert.AreEqual("qst_clear_pass", failed);
            CollectionAssert.Contains(recorder.Failed, "qst_clear_pass");
        }

        [Test]
        public void CrossDialogue_QuestOfferedOnFirst_IsRestoredAndCompletedOnLater()
        {
            var registry = new LiveQuestRegistry();
            var recorder = new FakeRecorder();
            var runner = BuildRunnerWith(recorder, registry);

            // Platform A: the quest is offered (and registered), but not completed here.
            _fake.Script(FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"));
            runner.Begin(Cast(enemyId: null, quest: QuestWith()));
            var offered = runner.ActiveQuest;
            Assert.IsNotNull(offered);
            Assert.AreEqual(QuestState.Active, offered.State);
            Assert.IsTrue(registry.TryGet("qst_clear_pass", out _));

            // Platform B: a later dialogue carrying the same quest restores the SAME live instance
            // (no fresh mint, no second Start) and completes it.
            _fake.Script(FakeStoryManager.Frame.Line("done.", "complete-quest:"));
            runner.Begin(Cast(enemyId: null, quest: QuestWith()));
            Assert.AreSame(offered, runner.ActiveQuest);

            runner.Continue(); // advance to the complete-quest line
            Assert.AreEqual(QuestState.Completed, runner.ActiveQuest.State);
            Assert.AreEqual(1, recorder.Started.Count); // started once on offer, not again on restore
            CollectionAssert.Contains(recorder.Completed, "qst_clear_pass");
        }

        [Test]
        public void CompleteQuest_NoActiveQuest_WarnsAndNoOps()
        {
            _fake.Script(FakeStoryManager.Frame.Line("done.", "complete-quest:"));

            int before = _logger.Warnings.Count;
            _runner.Begin(Cast(enemyId: null, quest: null));

            Assert.IsNull(_runner.ActiveQuest);
            Assert.AreEqual(before + 1, _logger.Warnings.Count);
        }

        [Test]
        public void AdvanceObjective_AppliesObjectiveCompletionEffects()
        {
            var objective = new QuestObjective("step", "", QuestObjectiveKind.Choice, 1, new[] { SetPassCleared() });
            _fake.Script(
                FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"),
                FakeStoryManager.Frame.Line("did it.", "advance-objective: step"));

            var runner = BuildRunnerWith(new FakeRecorder());
            runner.Begin(Cast(enemyId: null, quest: QuestWith(new[] { objective })));
            runner.Continue(); // advance to the advance-objective line

            Assert.IsTrue(runner.ActiveQuest.IsObjectiveComplete("step"));
            Assert.AreEqual(QuestState.Active, runner.ActiveQuest.State); // advancing never completes the quest
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
        }

        [Test]
        public void MultiLineKnot_EmitsOneLineAtATime_GatedByContinue()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("Line one."),
                FakeStoryManager.Frame.Line("Line two."));

            var lines = new List<string>();
            _runner.OnLine += l => lines.Add(l);

            _runner.Begin(Cast(enemyId: null, quest: null));
            Assert.AreEqual(new[] { "Line one." }, lines);
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State);

            _runner.Continue();
            Assert.AreEqual(new[] { "Line one.", "Line two." }, lines);
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State);

            _runner.Continue();
            Assert.AreEqual(2, lines.Count); // no more lines emitted
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
        }

        [Test]
        public void NoTextTagSteps_DoNotGate_OnlyTheTextLineGates()
        {
            // A speaker-only step carries no visible text, so it must flow into the next line, not gate.
            _fake.Script(
                FakeStoryManager.Frame.Line("", "speaker: Razor"),
                FakeStoryManager.Frame.Line("Razor speaks."));

            string speaker = null;
            var lines = new List<string>();
            _runner.OnSpeakerChanged += s => speaker = s;
            _runner.OnLine += l => lines.Add(l);

            _runner.Begin(Cast(enemyId: null, quest: null));
            Assert.AreEqual("Razor", speaker);
            Assert.AreEqual(new[] { "Razor speaks." }, lines);
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State);
        }

        [Test]
        public void Continue_WhenNotGated_IsNoOp()
        {
            // Before any conversation the runner is Ended; Continue must do nothing.
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            _runner.Continue();
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);

            _fake.Script(FakeStoryManager.Frame.Line("only line"));
            _runner.Begin(Cast(enemyId: null, quest: null)); // gates on the line
            _runner.Continue(); // advance to the end
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);

            // Continue while Ended is a no-op (no throw, stays Ended).
            _runner.Continue();
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
        }

        [Test]
        public void CombatAvailable_And_EnemyId_ReflectTheCasting()
        {
            _fake.Script(FakeStoryManager.Frame.Line("hi"));
            _runner.Begin(Cast("enemy_brute", null)); // combat slot filled
            Assert.IsTrue(_runner.CombatAvailable);
            Assert.AreEqual("enemy_brute", _runner.CombatEnemyId);

            _fake.Script(FakeStoryManager.Frame.Line("hi"));
            _runner.Begin(Cast(enemyId: null, quest: null)); // combat slot empty
            Assert.IsFalse(_runner.CombatAvailable);
            Assert.IsNull(_runner.CombatEnemyId);
        }

        [Test]
        public void TriggerCombat_Suspends_AndEmitsCombatEnemyId()
        {
            _fake.Script(FakeStoryManager.Frame.Line("The raider blocks the road."));
            string enemy = null;
            CombatInitiator? initiator = null;
            _runner.OnCombatTriggered += (e, who) => { enemy = e; initiator = who; };

            _runner.Begin(Cast("enemy_brute", null)); // gates on the line
            _runner.TriggerCombat();

            Assert.AreEqual(DialogueRunnerState.AwaitingExternal, _runner.State);
            Assert.AreEqual("enemy_brute", enemy);
            Assert.AreEqual(CombatInitiator.Player, initiator); // the Attack card = the player chose to attack

            // The existing combat resume path then applies (the Monster verb reuses start-combat's machinery).
            _runner.ReportCombatResult(true);
            Assert.AreEqual(true, _fake.GetVariable("combat_won"));
        }

        [Test]
        public void TriggerCombat_NoCombatAvailable_IsNoOpAndWarns()
        {
            _fake.Script(FakeStoryManager.Frame.Line("just a chat"));
            bool combatRaised = false;
            _runner.OnCombatTriggered += (_, __) => combatRaised = true;

            _runner.Begin(Cast(enemyId: null, quest: null)); // no combat slot
            int before = _logger.Warnings.Count;
            _runner.TriggerCombat();

            Assert.IsFalse(combatRaised);
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State); // unchanged
            Assert.AreEqual(before + 1, _logger.Warnings.Count);
        }

        [Test]
        public void Leave_EndsConversation_WithLeaveOutcome()
        {
            _fake.Script(FakeStoryManager.Frame.Line("hello"));
            string ended = null;
            _runner.OnDialogueEnded += o => ended = o;

            _runner.Begin(Cast(enemyId: null, quest: null)); // gates on the line
            _runner.Leave();

            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.AreEqual("leave", ended);
        }

        [Test]
        public void Leave_WhileSuspendedOnCombat_IsRefused()
        {
            _fake.Script(FakeStoryManager.Frame.Line("The raider blocks the road."));
            _runner.Begin(Cast("enemy_brute", null));
            _runner.TriggerCombat(); // -> AwaitingExternal

            int before = _logger.Warnings.Count;
            _runner.Leave();

            Assert.AreEqual(DialogueRunnerState.AwaitingExternal, _runner.State); // not ended
            Assert.AreEqual(before + 1, _logger.Warnings.Count);
        }
    }
}
