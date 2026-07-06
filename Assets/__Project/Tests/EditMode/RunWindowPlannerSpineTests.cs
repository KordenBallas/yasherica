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
    /// The spine reserved lane (D7, P3-1): spine beats place first by their own quota — outside the
    /// quest rarity/spacing gate and the live-thread ceiling — throttled by the per-run reveal cap,
    /// drawn from a precondition-gated pool (never an authored order), at most one per window, never
    /// twice, seeded-deterministic. The cap derives from the run ledger, so it holds across a restore.
    /// </summary>
    [TestFixture]
    public class RunWindowPlannerSpineTests
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

        /// <summary>The quest gate never grants: rarity and spacing are prohibitive, ambient stays
        /// empty. Anything narrative that still lands can only have come through the reserved lane.</summary>
        private static readonly WorldContentDensitySettings NoQuests =
            new WorldContentDensitySettings(averagePlatformsPerQuest: 100, minPlatformsBetweenQuests: 100,
                emptyWeight: 1, lootWeight: 0, combatWeight: 0);

        /// <summary>Every slot with an eligible story becomes a quest slot (the legacy fill mode).</summary>
        private static readonly WorldContentDensitySettings QuestEverywhere =
            new WorldContentDensitySettings(averagePlatformsPerQuest: 1, minPlatformsBetweenQuests: 0,
                emptyWeight: 0, lootWeight: 0, combatWeight: 0);

        private FactStore _store;
        private PreconditionEvaluator _evaluator;
        private FakeLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "run_escalation_tier", FactScope.Global, FactValueType.Int, FactValue.FromInt(0)),
                // The meta-horizon run counter spine soft floors read ("not before run N").
                new FactKeyInfo(FactNamespace.World, "run_count", FactScope.Global, FactValueType.Int, FactValue.FromInt(0)),
                new FactKeyInfo(FactNamespace.World, "saw_hint", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false))
            });
            _store = new FactStore(registry, _logger);
            _evaluator = new PreconditionEvaluator(new SubjectResolver(_logger), registry, _logger);
        }

        // A "not before run N" soft floor — an ordinary Gte predicate over the run counter (FR7).
        private static FactPredicate RunCountAtLeast(int n) =>
            new FactPredicate(FactNamespace.World, "", "run_count", ComparisonOp.Gte, FactValue.FromInt(n));

        private static StoryTemplateData Spine(string id, FactPredicate precondition = null,
            string thread = "", RunTierBand tierBand = default)
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            var preconditions = precondition != null ? new[] { precondition } : Array.Empty<FactPredicate>();
            return new StoryTemplateData(id, slots, preconditions, null, new[] { "spine" }, thread,
                true, weight: 10, tierBand: tierBand);
        }

        private static StoryTemplateData Ordinary(string id, string thread = "")
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            return new StoryTemplateData(id, slots, Array.Empty<FactPredicate>(), null,
                new[] { "bandit" }, thread, false, weight: 10);
        }

        private static NpcArchetypeData Archetype(string id, params string[] tags) =>
            new NpcArchetypeData(id, new[] { "Name" }, "faction", 0, tags);

        private static RunPacingSettings Settings(int windowSize = 4, int cap = 2, int maxLiveThreads = 3) =>
            new RunPacingSettings(windowSize, lookAheadWindows: 1, maxLiveThreads: maxLiveThreads,
                defaultThreadLifespanWindows: 3, maxSpineRevealsPerRun: cap);

        private RunWindowPlanner Planner(RunPacingSettings settings, IReadOnlyList<StoryTemplateData> stories,
            IReadOnlyList<NpcArchetypeData> archetypes, ulong seed = 7,
            WorldContentDensitySettings density = null, IThreadLedger threadLedger = null,
            IStoryRunLedger storyLedger = null)
        {
            var random = new DeterministicRandom(seed);
            var actorFactory = new ActorInstanceFactory(random);
            var liveActors = new LiveActorRegistry();
            var effectiveDensity = density ?? NoQuests;
            var pools = new BiomeMonsterPoolCatalog((Dictionary<LevelTheme, IReadOnlyList<int>>)null);
            var themes = new FakeThemeProvider();
            var catalog = new Sites.SiteCatalog(null);
            var inner = new WorldContentAllocator(effectiveDensity, pools, themes, random, _logger);
            var allocator = new SiteAwareSlotAllocator(inner, catalog,
                new Sites.SiteBlockBuilder(), effectiveDensity, pools, themes, random, _logger);
            var threads = threadLedger ?? new ThreadLedger();
            var storyRun = storyLedger ?? new StoryRunLedger();
            var definitions = new ThreadCatalog(null, settings.DefaultThreadLifespanWindows);
            var maintenance = new ThreadMaintenanceService(threads, definitions, _evaluator, _logger);
            return new RunWindowPlanner(stories, archetypes, _evaluator, actorFactory, liveActors, random,
                settings, allocator, threads, storyRun, definitions, maintenance, catalog, _logger);
        }

        private static int SpineCount(WindowPlan plan) =>
            plan.Platforms.Count(p => p.Kind == PlannedPlatformKind.Story && p.Story.IsSpine);

        private static int StoryCount(WindowPlan plan) =>
            plan.Platforms.Count(p => p.Kind == PlannedPlatformKind.Story);

        private int TotalSpineOverWindows(RunWindowPlanner planner, int windows, out int maxPerWindow)
        {
            int total = 0;
            maxPerWindow = 0;
            for (int window = 0; window < windows; window++)
            {
                int inWindow = SpineCount(planner.PlanWindow(window, _store));
                total += inWindow;
                maxPerWindow = Math.Max(maxPerWindow, inWindow);
            }

            return total;
        }

        [Test]
        public void SpineBeat_PlacesEvenWhenTheQuestGateDenies()
        {
            // Reserve, don't compete: with rarity/spacing prohibitive (no quest is ever granted), a
            // window full of ordinary content still yields its spine beat through the reserved lane.
            var planner = Planner(Settings(), new[] { Spine("spine_a") }, new[] { Archetype("a", "spine") });

            Assert.AreEqual(1, SpineCount(planner.PlanWindow(0, _store)));
        }

        [Test]
        public void RevealCap_IsEnforcedAcrossWindows()
        {
            var stories = new[] { Spine("spine_a"), Spine("spine_b") };
            var planner = Planner(Settings(cap: 1), stories, new[] { Archetype("a", "spine") });

            Assert.AreEqual(1, TotalSpineOverWindows(planner, windows: 4, out _),
                "With cap 1 exactly one reveal may land over the whole run.");
        }

        [Test]
        public void CapZero_DisablesTheLane()
        {
            var planner = Planner(Settings(cap: 0), new[] { Spine("spine_a") }, new[] { Archetype("a", "spine") });

            Assert.AreEqual(0, TotalSpineOverWindows(planner, windows: 3, out _));
        }

        [Test]
        public void AtMostOneRevealPerWindow_EvenWhenTheCapAllowsTwo()
        {
            // Two eligible beats + cap 2: both land eventually, but never side by side in one window.
            var stories = new[] { Spine("spine_a"), Spine("spine_b") };
            var planner = Planner(Settings(cap: 2), stories, new[] { Archetype("a", "spine") });

            int total = TotalSpineOverWindows(planner, windows: 5, out int maxPerWindow);
            Assert.AreEqual(2, total);
            Assert.AreEqual(1, maxPerWindow, "Reveals must not volley within a single window.");
        }

        [Test]
        public void ABeatIsNeverRevealedTwice()
        {
            // One spine story, cap 2: the second allowance must not re-place the same beat.
            var planner = Planner(Settings(cap: 2), new[] { Spine("spine_a") }, new[] { Archetype("a", "spine") });

            Assert.AreEqual(1, TotalSpineOverWindows(planner, windows: 4, out _));
        }

        [Test]
        public void RestoredLedger_SpendsTheCap_AndBlocksRePlacement()
        {
            // A continue restores the run ledger (P2-2); a reveal already placed before the save must
            // both count against the cap and never re-enter the pool.
            var restored = new StoryRunLedger();
            restored.Restore(new[] { new StoryRunEntry("spine_a", "", StoryRunStatus.Placed, 0) });
            var stories = new[] { Spine("spine_a"), Spine("spine_b") };

            var capOne = Planner(Settings(cap: 1), stories, new[] { Archetype("a", "spine") },
                storyLedger: restored);
            Assert.AreEqual(0, TotalSpineOverWindows(capOne, windows: 3, out _),
                "The restored reveal spent the whole cap.");

            var restoredAgain = new StoryRunLedger();
            restoredAgain.Restore(new[] { new StoryRunEntry("spine_a", "", StoryRunStatus.Placed, 0) });
            var capTwo = Planner(Settings(cap: 2), stories, new[] { Archetype("a", "spine") },
                storyLedger: restoredAgain);
            var plan = capTwo.PlanWindow(1, _store);
            Assert.AreEqual(1, SpineCount(plan));
            Assert.AreEqual("spine_b",
                plan.Platforms.First(p => p.Kind == PlannedPlatformKind.Story).Story.StoryId,
                "Only the un-revealed beat may place.");
        }

        [Test]
        public void SoftFloor_HoldsAHeavyRevealUntilTheRunCount()
        {
            // "Never too early" is per-beat (FR5): the heavy reveal carries a run-count floor and
            // cannot surface in an earlier run, no matter the cap or the pool.
            var heavy = Spine("spine_mirror", RunCountAtLeast(3));
            var planner = Planner(Settings(), new[] { heavy }, new[] { Archetype("a", "spine") });

            _store.SetInt(WorldFacts.RunCount, 1);
            Assert.AreEqual(0, SpineCount(planner.PlanWindow(0, _store)), "Run 1 is below the floor.");

            _store.SetInt(WorldFacts.RunCount, 3);
            Assert.AreEqual(1, SpineCount(planner.PlanWindow(1, _store)), "Run 3 satisfies the floor.");
        }

        [Test]
        public void SpineStories_NeverFillQuestSlots()
        {
            // With the lane off and quest slots everywhere, a spine story must still never place:
            // the reserved lane is its only entry path, else reveals leak past the cap.
            var planner = Planner(Settings(cap: 0), new[] { Spine("spine_a") },
                new[] { Archetype("a", "spine") }, density: QuestEverywhere);

            Assert.AreEqual(0, StoryCount(planner.PlanWindow(0, _store)));
        }

        [Test]
        public void SameSeed_ProducesIdenticalPlans_IncludingTheSpineSlot()
        {
            var stories = new StoryTemplateData[]
            {
                Spine("spine_a"), Spine("spine_b"), Ordinary("s1"), Ordinary("s2")
            };
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 2,
                minPlatformsBetweenQuests: 1, emptyWeight: 1, lootWeight: 1, combatWeight: 0);
            var archetypes = new[] { Archetype("a", "bandit") };

            var plannerA = Planner(Settings(windowSize: 6), stories, archetypes, seed: 42, density: density);
            var plannerB = Planner(Settings(windowSize: 6), stories, archetypes, seed: 42, density: density);

            for (int window = 0; window < 3; window++)
            {
                var sigA = plannerA.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                var sigB = plannerB.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                CollectionAssert.AreEqual(sigA, sigB, $"Window {window} diverged for the same seed.");
            }
        }

        [Test]
        public void AnInactiveLane_DrawsNothing_KeepingPlansIdenticalToASpineFreePool()
        {
            // A floor-gated (ineligible) spine story must leave the seeded stream untouched, so the
            // rest of the plan is bit-identical to a build with no spine content at all.
            var withGatedSpine = new StoryTemplateData[]
            {
                Spine("spine_mirror", RunCountAtLeast(99)), Ordinary("s1"), Ordinary("s2")
            };
            var withoutSpine = new StoryTemplateData[] { Ordinary("s1"), Ordinary("s2") };
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 2,
                minPlatformsBetweenQuests: 1, emptyWeight: 1, lootWeight: 1, combatWeight: 0);
            var archetypes = new[] { Archetype("a", "bandit") };

            var plannerA = Planner(Settings(windowSize: 6), withGatedSpine, archetypes, seed: 11, density: density);
            var plannerB = Planner(Settings(windowSize: 6), withoutSpine, archetypes, seed: 11, density: density);

            for (int window = 0; window < 3; window++)
            {
                var sigA = plannerA.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                var sigB = plannerB.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                CollectionAssert.AreEqual(sigA, sigB, $"Window {window}: the inactive lane leaked a draw.");
            }
        }

        [Test]
        public void TierBand_GatesTheSpinePool()
        {
            // The D19 register shift applies to spine beats like everyone else.
            var courtly = Spine("spine_courts", tierBand: new RunTierBand(3, 0));
            var planner = Planner(Settings(), new[] { courtly }, new[] { Archetype("a", "spine") });

            _store.SetInt(WorldFacts.RunEscalationTier, 0);
            Assert.AreEqual(0, SpineCount(planner.PlanWindow(0, _store)));

            _store.SetInt(WorldFacts.RunEscalationTier, 3);
            Assert.AreEqual(1, SpineCount(planner.PlanWindow(1, _store)));
        }

        [Test]
        public void ARetiredThreadsSpineBeat_NeverPlaces()
        {
            var threads = new ThreadLedger();
            threads.Open("spine_arc", ThreadKind.Arc, 0);
            threads.Fail("spine_arc", ThreadRetirementReason.Conflict);
            var planner = Planner(Settings(), new[] { Spine("spine_a", thread: "spine_arc") },
                new[] { Archetype("a", "spine") }, threadLedger: threads);

            Assert.AreEqual(0, TotalSpineOverWindows(planner, windows: 3, out _));
        }

        [Test]
        public void TheLane_BypassesTheLiveThreadCeiling()
        {
            // Reserve, don't compete: at the D14 ceiling a quest-channel opener would wait, but the
            // lane still places its beat and opens the thread — the reveal cap bounds the extra load.
            var threads = new ThreadLedger();
            threads.Open("occupied", ThreadKind.Arc, 0);
            var planner = Planner(Settings(maxLiveThreads: 1), new[] { Spine("spine_a", thread: "spine_arc") },
                new[] { Archetype("a", "spine") }, threadLedger: threads);

            Assert.AreEqual(1, SpineCount(planner.PlanWindow(0, _store)));
            Assert.IsTrue(threads.TryGet("spine_arc", out var record));
            Assert.AreEqual(ThreadState.Live, record.State);
        }

        [Test]
        public void DefaultSettings_CapIsTwo_AndNegativeClampsToZero()
        {
            Assert.AreEqual(2, new RunPacingSettings(4, 1).MaxSpineRevealsPerRun);
            Assert.AreEqual(0, new RunPacingSettings(4, 1, maxSpineRevealsPerRun: -5).MaxSpineRevealsPerRun);
        }
    }
}
