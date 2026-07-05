using System;
using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using LevelGeneration;
using Loot.Core;
using Narrative.Actors.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using Narrative.Threads.Core;
using NUnit.Framework;
using Sites = World.Sites.Core;

namespace Tests.EditMode
{
    /// <summary>
    /// Planner-level acceptance tests for first-class threads (P2-3): no stale re-placement (FR9),
    /// causal-order holding (FR8), the concurrency ceiling with advance-over-open (FR3/FR7), and
    /// conflict/expiry retirement flowing through window planning (FR4/FR5).
    /// </summary>
    [TestFixture]
    public class RunWindowPlannerThreadTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeThemeProvider : ICurrentThemeProvider
        {
            public LevelTheme CurrentTheme => LevelTheme.Forest;
            public void SetTheme(LevelTheme theme) { }
        }

        private static readonly WorldContentDensitySettings QuestEverywhere =
            new WorldContentDensitySettings(averagePlatformsPerQuest: 1, minPlatformsBetweenQuests: 0,
                emptyWeight: 0, lootWeight: 0, combatWeight: 0);

        private FactStore _store;
        private PreconditionEvaluator _evaluator;
        private FakeLogger _logger;
        private ThreadLedger _threads;
        private StoryRunLedger _storyRun;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "cause_done", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "joined_lizards", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, ThreadFactKeys.Retired, FactScope.PerThread, FactValueType.String, FactValue.FromString(""))
            });
            _store = new FactStore(registry, _logger);
            _evaluator = new PreconditionEvaluator(new SubjectResolver(_logger), registry, _logger);
            _threads = new ThreadLedger();
            _storyRun = new StoryRunLedger();
        }

        private static FactPredicate World(string key, bool value) =>
            new FactPredicate(FactNamespace.World, "", key, ComparisonOp.Eq, FactValue.FromBool(value));

        private static StoryTemplateData Story(string id, string thread = "", FactPredicate precondition = null,
            string tag = "villager")
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            var preconditions = precondition != null ? new[] { precondition } : Array.Empty<FactPredicate>();
            return new StoryTemplateData(id, slots, preconditions, null, new[] { tag }, thread, false, weight: 10);
        }

        private static NpcArchetypeData Archetype(string id, params string[] tags) =>
            new NpcArchetypeData(id, new[] { "Name" }, "faction", 0, tags);

        private RunWindowPlanner Planner(RunPacingSettings settings, IReadOnlyList<StoryTemplateData> stories,
            IThreadCatalog threadCatalog = null, ulong seed = 7)
        {
            var random = new DeterministicRandom(seed);
            var actorFactory = new ActorInstanceFactory(random);
            var themes = new FakeThemeProvider();
            var pools = new BiomeMonsterPoolCatalog((Dictionary<LevelTheme, IReadOnlyList<int>>)null);
            var catalog = new Sites.SiteCatalog(null);
            var inner = new WorldContentAllocator(QuestEverywhere, pools, themes, random, _logger);
            var allocator = new SiteAwareSlotAllocator(inner, catalog,
                new Sites.SiteBlockBuilder(), QuestEverywhere, pools, themes, random, _logger);
            var definitions = threadCatalog ?? new ThreadCatalog(null, settings.DefaultThreadLifespanWindows);
            var maintenance = new ThreadMaintenanceService(_threads, definitions, _evaluator, _logger);
            return new RunWindowPlanner(stories, new[] { Archetype("arch", "villager") }, _evaluator,
                actorFactory, new LiveActorRegistry(), random, settings, allocator,
                _threads, _storyRun, definitions, maintenance, catalog, _logger);
        }

        private static string[] StoryIds(WindowPlan plan) =>
            plan.Platforms.Where(p => p.Kind == PlannedPlatformKind.Story)
                .Select(p => p.Story.StoryId).ToArray();

        [Test]
        public void PlacedStory_NotRePlacedNextWindow_EvenWithTruePreconditions()
        {
            // THE FR9 bug-fix proof: before the run-scoped story ledger, an always-eligible story
            // placed in window 0 (not yet resolved, so no fact flipped) re-placed as fresh in window 1.
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var planner = Planner(settings, new[] { Story("opener", thread: "t") });

            CollectionAssert.Contains(StoryIds(planner.PlanWindow(0, _store)), "opener");
            CollectionAssert.DoesNotContain(StoryIds(planner.PlanWindow(1, _store)), "opener");
        }

        [Test]
        public void ResolvedStory_NeverRePlaced()
        {
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var planner = Planner(settings, new[] { Story("legacy") });
            _storyRun.NoteResolved("legacy"); // resolved via a path the planner never placed

            Assert.AreEqual(0, StoryIds(planner.PlanWindow(0, _store)).Length);
        }

        [Test]
        public void FailedThreadBeat_NeverPlaced()
        {
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var planner = Planner(settings, new[] { Story("beat2", thread: "t") });
            _threads.Open("t", ThreadKind.Ephemeral, 0);
            _threads.Fail("t", ThreadRetirementReason.Conflict);

            Assert.AreEqual(0, StoryIds(planner.PlanWindow(0, _store)).Length);
        }

        [Test]
        public void AmbientColourStory_StaysOutsideTheRunLedger()
        {
            // S11: chatter is not a story beat - placing it must not record run-state (it may repeat).
            var settings = new RunPacingSettings(windowSize: 3, lookAheadWindows: 1);
            var site = new Sites.SiteDefinitionData("village", "settlement", 3, 3, triggerWeight: 1,
                new[] { new Sites.ContentBeat(Sites.ContentBaseKind.Npc, "quest-bearer") }, 2, 2,
                new[] { new Sites.WeightedBeat(new Sites.ContentBeat(Sites.ContentBaseKind.Npc, "townsfolk"), 1) },
                "kit");
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 1,
                minPlatformsBetweenQuests: 100, emptyWeight: 1, lootWeight: 0, combatWeight: 0,
                averagePlatformsPerAmbientSite: 0, minPlatformsBetweenSites: 0, wildQuestWeight: 0);
            var random = new DeterministicRandom(7);
            var themes = new FakeThemeProvider();
            var pools = new BiomeMonsterPoolCatalog((Dictionary<LevelTheme, IReadOnlyList<int>>)null);
            var catalog = new Sites.SiteCatalog(new[] { site });
            var inner = new WorldContentAllocator(density, pools, themes, random, _logger);
            var allocator = new SiteAwareSlotAllocator(inner, catalog,
                new Sites.SiteBlockBuilder(), density, pools, themes, random, _logger);
            var definitions = new ThreadCatalog(null, 3);
            var planner = new RunWindowPlanner(
                new[] { Story("quest"), Story("chatter", tag: "townsfolk") },
                new[] { Archetype("arch", "villager") }, _evaluator,
                new ActorInstanceFactory(random), new LiveActorRegistry(), random, settings, allocator,
                _threads, _storyRun, definitions,
                new ThreadMaintenanceService(_threads, definitions, _evaluator, _logger), catalog, _logger);

            var plan = planner.PlanWindow(0, _store);

            Assert.IsTrue(plan.Platforms.Any(p => p.Kind == PlannedPlatformKind.Story && p.Story.StoryId == "chatter"),
                "The site fill should place the chatter story.");
            Assert.IsTrue(_storyRun.IsPlacedOrResolved("quest"));
            Assert.IsFalse(_storyRun.IsPlacedOrResolved("chatter"), "Chatter must stay outside the run ledger.");
        }

        [Test]
        public void ConsequenceBeat_HeldUntilCauseFactLive_NeverBeforeCause()
        {
            // FR8: the consequence gates on the fact its cause writes at RESOLUTION. Planning one
            // window ahead, the consequence must not appear until the fact is live - landing a window
            // later than the cause is the accepted thinner window.
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var cause = Story("cause", thread: "t");
            var consequence = Story("consequence", thread: "t", precondition: World("cause_done", true));
            var planner = Planner(settings, new[] { cause, consequence });

            var window0 = StoryIds(planner.PlanWindow(0, _store));
            CollectionAssert.AreEqual(new[] { "cause" }, window0); // consequence held, never co-placed

            // The player has not engaged the cause yet: still held.
            var window1 = StoryIds(planner.PlanWindow(1, _store));
            CollectionAssert.DoesNotContain(window1, "consequence");

            // Cause resolves: fact written, thread advance noted (what the resolution relay does).
            _store.Set(FactKey.Global(FactNamespace.World, "cause_done"), FactValue.FromBool(true));
            _storyRun.NoteResolved("cause");
            _threads.NoteBeatResolved("t");

            var window2 = StoryIds(planner.PlanWindow(2, _store));
            CollectionAssert.AreEqual(new[] { "consequence" }, window2); // after the cause, exactly once
        }

        [Test]
        public void AtCap_NewThreadOpenerNotPlaced_SlotDegrades()
        {
            // FR7: at the ceiling no new thread opens - the quest slot degrades into the ambient draw
            // (waits) instead of force-dropping an engaged thread.
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1, maxLiveThreads: 1);
            var planner = Planner(settings, new[] { Story("openA", thread: "a"), Story("openB", thread: "b") });

            var placed = StoryIds(planner.PlanWindow(0, _store));

            Assert.AreEqual(1, placed.Length, "With cap 1 only one thread may open.");
            Assert.AreEqual(1, _threads.LiveCount);
        }

        [Test]
        public void AtCap_AdvancingBeatStillPlaced()
        {
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1, maxLiveThreads: 1);
            var beat2 = Story("beat2", thread: "a", precondition: World("cause_done", true));
            var planner = Planner(settings, new[] { Story("beat1", thread: "a"), beat2 });

            CollectionAssert.AreEqual(new[] { "beat1" }, StoryIds(planner.PlanWindow(0, _store)));
            _store.Set(FactKey.Global(FactNamespace.World, "cause_done"), FactValue.FromBool(true));
            _threads.NoteBeatResolved("a");

            // Cap is reached (thread a is the one live thread), yet its own next beat still places.
            CollectionAssert.AreEqual(new[] { "beat2" }, StoryIds(planner.PlanWindow(1, _store)));
        }

        [Test]
        public void BelowCap_AdvancePreferredOverOpen()
        {
            // FR3: with room to open a new thread, advancing the live one still wins the pick.
            // openB shares beat2's gate so that in window 1 exactly two candidates compete for the
            // single slot: thread a's next beat (live) vs thread b's opener (new).
            var settings = new RunPacingSettings(windowSize: 1, lookAheadWindows: 1, maxLiveThreads: 3);
            var beat2 = Story("beat2", thread: "a", precondition: World("cause_done", true));
            var opener = Story("openB", thread: "b", precondition: World("cause_done", true));
            var planner = Planner(settings, new[] { Story("beat1", thread: "a"), beat2, opener });

            CollectionAssert.AreEqual(new[] { "beat1" }, StoryIds(planner.PlanWindow(0, _store)));
            _store.Set(FactKey.Global(FactNamespace.World, "cause_done"), FactValue.FromBool(true));
            _threads.NoteBeatResolved("a");

            // Window 1 has one slot and two candidates; the live thread's beat must be preferred.
            CollectionAssert.AreEqual(new[] { "beat2" }, StoryIds(planner.PlanWindow(1, _store)));
        }

        [Test]
        public void ConflictedThread_StoriesExcluded_IndicatorWritten_NoClosureBeat()
        {
            // FR5/FR6: the premise contradiction retires the thread at the next tick; its remaining
            // beats never place, the indicator fact is written, and no closure beat appears.
            var catalog = new ThreadCatalog(new[]
            {
                new ThreadDefinitionData("fox", ThreadKind.Ephemeral,
                    new[] { World("joined_lizards", false) }, null, lifespanWindows: 5)
            }, defaultLifespanWindows: 3);
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var beat2 = Story("fox_beat2", thread: "fox", precondition: World("cause_done", true));
            var planner = Planner(settings, new[] { Story("fox_open", thread: "fox"), beat2 }, catalog);

            CollectionAssert.AreEqual(new[] { "fox_open" }, StoryIds(planner.PlanWindow(0, _store)));

            // The player closes the door; the held beat's own gate even becomes true.
            _store.Set(FactKey.Global(FactNamespace.World, "joined_lizards"), FactValue.FromBool(true));
            _store.Set(FactKey.Global(FactNamespace.World, "cause_done"), FactValue.FromBool(true));

            var window1 = planner.PlanWindow(1, _store);
            Assert.AreEqual(0, StoryIds(window1).Length, "No beat of the failed thread, no closure beat.");
            Assert.IsTrue(_threads.TryGet("fox", out var record));
            Assert.AreEqual(ThreadState.Failed, record.State);
            Assert.AreEqual(ThreadRetirementReason.Conflict, record.Reason);
            Assert.AreEqual(ThreadFactKeys.RetiredValueConflict, _store.GetOrDefault(
                new FactKey(FactNamespace.World, "fox", ThreadFactKeys.Retired), FactValue.FromString("")).AsString());
        }

        [Test]
        public void EphemeralThread_ExpiresThroughPlannerTicks_ArcDoesNot()
        {
            var catalog = new ThreadCatalog(new[]
            {
                new ThreadDefinitionData("errand", ThreadKind.Ephemeral, null, null, lifespanWindows: 1),
                new ThreadDefinitionData("saga", ThreadKind.Arc, null, null, lifespanWindows: 1)
            }, defaultLifespanWindows: 3);
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var planner = Planner(settings,
                new[] { Story("errand_open", thread: "errand"), Story("saga_open", thread: "saga") }, catalog);

            var window0 = StoryIds(planner.PlanWindow(0, _store));
            Assert.AreEqual(2, window0.Length); // both threads open (cap default 3)

            planner.PlanWindow(1, _store); // 1 window without advance - at the lifespan, still live
            Assert.IsFalse(_threads.IsRetired("errand"));

            planner.PlanWindow(2, _store); // 2 > lifespan - the errand lapses silently; the arc holds
            Assert.IsTrue(_threads.IsRetired("errand"));
            Assert.IsTrue(_threads.TryGet("errand", out var errand));
            Assert.AreEqual(ThreadRetirementReason.Expired, errand.Reason);
            Assert.IsFalse(_threads.IsRetired("saga"));
        }

        [Test]
        public void SameSeedAndSameResolutions_IdenticalThreadLifecycle()
        {
            // FR12: two identically-seeded stacks fed the same resolutions replay the same lifecycle.
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1, maxLiveThreads: 2);
            var stories = new[]
            {
                Story("openA", thread: "a"),
                Story("beatA2", thread: "a", precondition: World("cause_done", true)),
                Story("openB", thread: "b"),
                Story("openC", thread: "c")
            };

            var timelines = new List<string>();
            for (int replay = 0; replay < 2; replay++)
            {
                SetUp(); // fresh store + ledgers per replay
                var planner = Planner(settings, stories, seed: 42);
                var timeline = new List<string>();
                for (int window = 0; window < 4; window++)
                {
                    if (window == 2)
                    {
                        _store.Set(FactKey.Global(FactNamespace.World, "cause_done"), FactValue.FromBool(true));
                        _threads.NoteBeatResolved("a");
                    }

                    timeline.Add($"w{window}:{string.Join(",", StoryIds(planner.PlanWindow(window, _store)))}");
                }

                foreach (var record in _threads.Threads)
                {
                    timeline.Add($"{record.ThreadId}:{record.State}:{record.Reason}:{record.Stage}");
                }

                timelines.Add(string.Join("|", timeline));
            }

            Assert.AreEqual(timelines[0], timelines[1]);
        }
    }
}
