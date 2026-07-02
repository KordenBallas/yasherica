using System;
using System.Collections.Generic;
using Core.Logging;
using Narrative;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class EncounterDirectorTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private static readonly FactKeyShapeCore PassClearedShape =
            new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool);

        private FactStore _store;
        private FakeStoryManager _fake;
        private DialogueRunner _runner;
        private RunDirector _runDirector;
        private CastingFactory _castingFactory;
        private FragmentLibrary _library;

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false))
            });
            _store = new FactStore(registry, logger);
            var resolver = new SubjectResolver(logger);
            var applier = new FactEffectApplier(resolver, logger);
            var evaluator = new PreconditionEvaluator(resolver, registry, logger);
            var parser = new DialogueTagParser(registry, logger);
            _fake = new FakeStoryManager();
            var session = new DialogueSession(_fake);
            _runner = new DialogueRunner(session, _store, applier, parser, recorder: null, logger: logger);
            _runDirector = new RunDirector(evaluator, new DeterministicRandom(7));
            _castingFactory = new CastingFactory(new DeterministicRandom(7), logger);
            _library = new FragmentLibrary(
                new[] { Dlg("dlg_toll", new[] { PassClearedShape }, "shakedown"), Dlg("dlg_caravan", Array.Empty<FactKeyShapeCore>(), "thanks") },
                null, null);
        }

        private static DialogueData Dlg(string id, FactKeyShapeCore[] writes, params string[] tags) =>
            new DialogueData(id, "{}", "start", Array.Empty<string>(), writes, tags);

        private static FactPredicate PassCleared(bool value) =>
            new FactPredicate(FactNamespace.World, "", "pass_cleared", ComparisonOp.Eq, FactValue.FromBool(value));

        private static StoryTemplateData Story(string id, string thread, string dialogueTag, FactPredicate precondition, bool optionalDialogue = false) =>
            new StoryTemplateData(id,
                new[] { new StorySlot("d", SlotKind.Dialogue, new[] { dialogueTag }, optionalDialogue) },
                new[] { precondition }, null, null, thread, false);

        private static NpcInstance Actor() => new NpcInstance("npc_07", "arch_road_bandit", "Razor", "free_blades");

        private EncounterDirector Director(params StoryTemplateData[] storylets) =>
            new EncounterDirector(_runDirector, _castingFactory, _library, storylets, _store, _runner);

        [Test]
        public void BeginEncounter_EligibleStorylet_CastsAndStartsDialogue()
        {
            _fake.Script(FakeStoryManager.Frame.Line("State your business."));
            var toll = Story("story_toll", "road", "shakedown", PassCleared(false)); // eligible: pass_cleared default false

            string line = null;
            _runner.OnLine += l => line = l;

            var started = Director(toll).BeginEncounter(Actor());

            Assert.IsTrue(started);
            Assert.AreEqual("State your business.", line);
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State);
        }

        [Test]
        public void BeginEncounter_NoEligibleStorylet_ReturnsFalse_RunnerUntouched()
        {
            var caravan = Story("story_caravan", "trade", "thanks", PassCleared(true)); // ineligible: pass_cleared is false

            var started = Director(caravan).BeginEncounter(Actor());

            Assert.IsFalse(started);
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State); // never began
        }

        [Test]
        public void BeginEncounter_EligibleButUncastable_ReturnsFalse()
        {
            // Eligible storylet, but its dialogue slot requires a tag no library fragment carries.
            var story = Story("story_orphan", "road", "missing_tag", PassCleared(false));

            var started = Director(story).BeginEncounter(Actor());

            Assert.IsFalse(started);
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
        }

        [Test]
        public void BeginEncounter_NullActor_ReturnsFalse()
        {
            var toll = Story("story_toll", "road", "shakedown", PassCleared(false));
            Assert.IsFalse(Director(toll).BeginEncounter(null));
        }

        [Test]
        public void FactWrittenInOneEncounter_ChangesEligibilityOfTheNext_ViaFacts()
        {
            // R7 end-to-end through the orchestrator: the toll dialogue writes world.pass_cleared, which
            // flips the toll storylet ineligible and the caravan storylet eligible — no story references
            // the other; coupling is purely through the shared fact, evaluated at the next encounter.
            var toll = Story("story_toll", "road", "shakedown", PassCleared(false));
            var caravan = Story("story_caravan", "trade", "thanks", PassCleared(true));
            var director = Director(toll, caravan);
            var actor = Actor();

            // Encounter 1: only the toll is eligible; its single line writes pass_cleared = true.
            _fake.Script(FakeStoryManager.Frame.Line("The road's yours.", "fact: world.pass_cleared Set true"));
            Assert.IsTrue(director.BeginEncounter(actor));
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
            _runner.Continue(); // drain encounter 1 to Ended (empties the scripted queue)
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);

            // Encounter 2: the toll is now ineligible (pass_cleared != false), so the caravan is chosen.
            string line = null;
            _runner.OnLine += l => line = l;
            _fake.Script(FakeStoryManager.Frame.Line("Bless you, traveller."));

            Assert.IsTrue(director.BeginEncounter(actor));
            Assert.AreEqual("Bless you, traveller.", line); // the caravan dialogue ran, not the toll
        }
    }
}
