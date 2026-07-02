using System.Collections.Generic;
using Core.Logging;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunDirectorTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private FactKeyRegistry _registry;
        private FactStore _store;
        private RunDirector _director;
        private FactEffectApplier _applier;
        private SubjectContext _context;

        private static readonly FactKeyShapeCore PassClearedShape =
            new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool);

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            _registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_blocked", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false))
            });
            _store = new FactStore(_registry, logger);
            var resolver = new SubjectResolver(logger);
            var evaluator = new PreconditionEvaluator(resolver, _registry, logger);
            _director = new RunDirector(evaluator, new DeterministicRandom(7));
            _applier = new FactEffectApplier(resolver, logger);
            _context = new SubjectContext();
        }

        private static FactPredicate Eq(string key, bool value) =>
            new FactPredicate(FactNamespace.World, "", key, ComparisonOp.Eq, FactValue.FromBool(value));

        private static StoryTemplateData Story(string id, string thread, FactPredicate precondition) =>
            new StoryTemplateData(id, null, new[] { precondition }, null, null, thread, false);

        [Test]
        public void SelectNext_OnlyReturnsEligibleStorylets()
        {
            var toll = Story("story_razor_pass_toll", "road", Eq("pass_blocked", true));
            var caravan = Story("story_grateful_caravan", "trade", Eq("pass_cleared", true));
            _store.Set(FactKey.Global(FactNamespace.World, "pass_blocked"), FactValue.FromBool(true));

            var selection = _director.SelectNext(new[] { toll, caravan }, _store, _context);

            Assert.AreEqual(1, selection.Eligible.Count);
            Assert.AreEqual("story_razor_pass_toll", selection.Chosen.StoryId);
        }

        [Test]
        public void SelectNext_NoneEligible_ReturnsNoSelection()
        {
            var caravan = Story("story_grateful_caravan", "trade", Eq("pass_cleared", true));
            var selection = _director.SelectNext(new[] { caravan }, _store, _context);
            Assert.IsFalse(selection.HasSelection);
            Assert.AreEqual(0, selection.Eligible.Count);
        }

        [Test]
        public void CrossStorylet_ChoiceInOneThreadChangesEligibilityInAnother_ViaFacts()
        {
            // R7 acceptance: clearing the pass (thread "road") makes the caravan storylet (thread
            // "trade") eligible, purely through the shared fact pass_cleared. No story references another.
            var toll = Story("story_razor_pass_toll", "road", Eq("pass_blocked", true));
            var caravan = Story("story_grateful_caravan", "trade", Eq("pass_cleared", true));
            _store.Set(FactKey.Global(FactNamespace.World, "pass_blocked"), FactValue.FromBool(true));

            var before = _director.SelectNext(new[] { toll, caravan }, _store, _context);
            Assert.IsFalse(Contains(before.Eligible, "story_grateful_caravan"));

            // The toll story's play-time effect writes pass_cleared = true (footprint-gated).
            var effect = new FactEffectCore(FactNamespace.World, "", "pass_cleared", FactEffectOp.Set, FactValue.FromBool(true));
            Assert.IsTrue(_applier.Apply(effect, _store, _context, new[] { PassClearedShape }));

            var after = _director.SelectNext(new[] { toll, caravan }, _store, _context);
            Assert.IsTrue(Contains(after.Eligible, "story_grateful_caravan"));
        }

        private static bool Contains(IReadOnlyList<StoryTemplateData> list, string id)
        {
            foreach (var s in list)
            {
                if (s.StoryId == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
