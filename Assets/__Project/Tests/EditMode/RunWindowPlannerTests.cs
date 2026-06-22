using System;
using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunWindowPlannerTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
        }

        private FactStore _store;
        private PreconditionEvaluator _evaluator;
        private FakeLogger _logger;

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
            _evaluator = new PreconditionEvaluator(resolver, registry, _logger);
        }

        private static FactPredicate PassCleared(bool value) =>
            new FactPredicate(FactNamespace.World, "", "pass_cleared", ComparisonOp.Eq, FactValue.FromBool(value));

        private static StoryTemplateData Story(string id, int weight, string[] tags, bool combat = false,
            FactPredicate precondition = null, string thread = "")
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            if (combat)
            {
                slots.Add(new StorySlot("c", SlotKind.Combat, new[] { "fight" }, false));
            }

            var preconditions = precondition != null ? new[] { precondition } : Array.Empty<FactPredicate>();
            return new StoryTemplateData(id, slots, preconditions, null, tags, thread, false, weight);
        }

        private static NpcArchetypeData Archetype(string id, params string[] tags) =>
            new NpcArchetypeData(id, new[] { "Name" }, "faction", 0, tags);

        private RunWindowPlanner Planner(RunPacingSettings settings, IReadOnlyList<StoryTemplateData> stories,
            IReadOnlyList<NpcArchetypeData> archetypes, ulong seed = 7)
        {
            var random = new DeterministicRandom(seed);
            var actorFactory = new ActorInstanceFactory(random);
            return new RunWindowPlanner(stories, archetypes, _evaluator, actorFactory, random, settings, _logger);
        }

        private static int StoryCount(WindowPlan plan) =>
            plan.Platforms.Count(p => p.Kind == PlannedPlatformKind.Story);

        [Test]
        public void NarrativeBudget_StopsFillAtCap()
        {
            var settings = new RunPacingSettings(windowSize: 5, narrativeBudgetPerWindow: 30,
                minCombatPerWindow: 0, maxCombatPerWindow: 0, lookAheadWindows: 1);
            var stories = new[]
            {
                Story("s1", 20, new[] { "bandit" }),
                Story("s2", 20, new[] { "bandit" }),
                Story("s3", 20, new[] { "bandit" })
            };
            var plan = Planner(settings, stories, new[] { Archetype("a", "bandit") }).PlanWindow(0, _store);

            Assert.AreEqual(5, plan.Platforms.Count);     // padded to window size
            Assert.AreEqual(1, StoryCount(plan));         // 20 + 20 would exceed the 30 budget
        }

        [Test]
        public void CombatBudget_MeetsMinimum()
        {
            var settings = new RunPacingSettings(windowSize: 3, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 1, maxCombatPerWindow: 3, lookAheadWindows: 1);
            var stories = new[]
            {
                Story("peace1", 10, new[] { "bandit" }),
                Story("peace2", 10, new[] { "bandit" }),
                Story("fight1", 10, new[] { "bandit" }, combat: true)
            };
            var plan = Planner(settings, stories, new[] { Archetype("a", "bandit") }).PlanWindow(0, _store);

            Assert.GreaterOrEqual(plan.CombatCount, 1);
        }

        [Test]
        public void CombatBudget_DoesNotExceedMaximum()
        {
            var settings = new RunPacingSettings(windowSize: 4, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 0, maxCombatPerWindow: 1, lookAheadWindows: 1);
            var stories = new[]
            {
                Story("fight1", 10, new[] { "bandit" }, combat: true),
                Story("fight2", 10, new[] { "bandit" }, combat: true),
                Story("fight3", 10, new[] { "bandit" }, combat: true)
            };
            var plan = Planner(settings, stories, new[] { Archetype("a", "bandit") }).PlanWindow(0, _store);

            Assert.LessOrEqual(plan.CombatCount, 1);
        }

        [Test]
        public void SameSeedAndFacts_ProduceIdenticalPlan()
        {
            var settings = new RunPacingSettings(windowSize: 4, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 0, maxCombatPerWindow: 4, lookAheadWindows: 1);
            var stories = new[]
            {
                Story("s1", 10, new[] { "bandit" }),
                Story("s2", 10, new[] { "bandit" }),
                Story("s3", 10, new[] { "bandit" }, combat: true)
            };
            var archetypes = new[] { Archetype("a", "bandit") };

            var planA = Planner(settings, stories, archetypes, seed: 42).PlanWindow(0, _store);
            var planB = Planner(settings, stories, archetypes, seed: 42).PlanWindow(0, _store);

            var idsA = planA.Platforms.Select(p => p.Story?.StoryId ?? "<empty>").ToArray();
            var idsB = planB.Platforms.Select(p => p.Story?.StoryId ?? "<empty>").ToArray();
            CollectionAssert.AreEqual(idsA, idsB);
        }

        [Test]
        public void PreconditionWrittenByPriorPlay_ChangesEligibilityNextWindow()
        {
            // R7: a story gated on world.pass_cleared == true is excluded until the fact is written.
            var settings = new RunPacingSettings(windowSize: 2, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 0, maxCombatPerWindow: 2, lookAheadWindows: 1);
            var caravan = Story("caravan", 10, new[] { "bandit" }, precondition: PassCleared(true));
            var planner = Planner(settings, new[] { caravan }, new[] { Archetype("a", "bandit") });

            var before = planner.PlanWindow(0, _store);
            Assert.AreEqual(0, StoryCount(before)); // pass_cleared is false -> ineligible

            _store.Set(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(true));
            var after = planner.PlanWindow(1, _store);
            Assert.AreEqual(1, StoryCount(after));  // now eligible
        }

        [Test]
        public void StoryWithoutTagOverlap_IsStillPlaced()
        {
            // Actor matching is a soft preference: a story whose tags don't overlap any archetype is still
            // placed by falling back to any available archetype.
            var settings = new RunPacingSettings(windowSize: 2, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 0, maxCombatPerWindow: 2, lookAheadWindows: 1);
            var ghostStory = Story("ghost", 10, new[] { "ghost" }); // no archetype carries "ghost"
            var plan = Planner(settings, new[] { ghostStory }, new[] { Archetype("a", "bandit") }).PlanWindow(0, _store);

            Assert.AreEqual(1, StoryCount(plan));
        }

        [Test]
        public void NoArchetypesAtAll_PlacesNothing()
        {
            var settings = new RunPacingSettings(windowSize: 2, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 0, maxCombatPerWindow: 2, lookAheadWindows: 1);
            var plan = Planner(settings, new[] { Story("s1", 10, new[] { "bandit" }) },
                Array.Empty<NpcArchetypeData>()).PlanWindow(0, _store);

            Assert.AreEqual(0, StoryCount(plan));
        }

        [Test]
        public void OverlappingArchetypePreferredOverNonMatching()
        {
            var settings = new RunPacingSettings(windowSize: 1, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 0, maxCombatPerWindow: 1, lookAheadWindows: 1);
            var archetypes = new[] { Archetype("arch_match", "bandit"), Archetype("arch_other", "beast") };
            var plan = Planner(settings, new[] { Story("s1", 10, new[] { "bandit" }) }, archetypes).PlanWindow(0, _store);

            var story = plan.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.AreEqual("arch_match", story.Actor.ArchetypeId);
        }

        [Test]
        public void PlacedStory_GetsAnAssignedActor()
        {
            var settings = new RunPacingSettings(windowSize: 1, narrativeBudgetPerWindow: 100,
                minCombatPerWindow: 0, maxCombatPerWindow: 1, lookAheadWindows: 1);
            var plan = Planner(settings, new[] { Story("s1", 10, new[] { "bandit" }) },
                new[] { Archetype("arch_bandit", "bandit") }).PlanWindow(0, _store);

            var story = plan.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.IsNotNull(story.Actor);
            Assert.AreEqual("arch_bandit", story.Actor.ArchetypeId);
        }
    }
}
