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
    /// The cross-run spine cursor + the meta consumers (D20, P3-3): a beat SEEN in an earlier run
    /// (its <c>world.&lt;storyId&gt;.spine_seen</c> meta fact is true) never re-enters the spine pool,
    /// while a beat merely placed returns after death; past-run reveals never spend a fresh run's
    /// cap; mirror-lore echo variants gate on persisted meta flags with authored sibling exclusion;
    /// an empty meta store degrades gracefully and everything stays seeded-deterministic.
    /// </summary>
    [TestFixture]
    public class RunWindowPlannerSpineCursorTests
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

        /// <summary>The quest gate never grants; anything narrative comes through the reserved lane.</summary>
        private static readonly WorldContentDensitySettings NoQuests =
            new WorldContentDensitySettings(averagePlatformsPerQuest: 100, minPlatformsBetweenQuests: 100,
                emptyWeight: 1, lootWeight: 0, combatWeight: 0);

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
                new FactKeyInfo(FactNamespace.World, "run_count", FactScope.Global, FactValueType.Int, FactValue.FromInt(0), FactHorizon.Meta),
                // The cross-run cursor: per-story seen flag (P3-3), restored by the meta bootstrap.
                new FactKeyInfo(FactNamespace.World, "spine_seen", FactScope.PerStory, FactValueType.Bool, FactValue.FromBool(false), FactHorizon.Meta),
                // Persisted key choices the mirror-lore echoes read (path lean / landmark deeds).
                new FactKeyInfo(FactNamespace.World, "raider_pact_sworn", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false), FactHorizon.Meta),
                new FactKeyInfo(FactNamespace.World, "barn_bounty_honored", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false), FactHorizon.Meta)
            });
            _store = new FactStore(registry, _logger);
            _evaluator = new PreconditionEvaluator(new SubjectResolver(_logger), registry, _logger);
        }

        private static FactPredicate WorldFlagIs(string key, bool value) =>
            new FactPredicate(FactNamespace.World, "", key, ComparisonOp.Eq, FactValue.FromBool(value));

        /// <summary>An authored "sibling exclusion" / "must have seen Y" gate over the cursor —
        /// a literal story-id subject token, exactly as a designer authors it (no code path).</summary>
        private static FactPredicate SpineSeenIs(string storyId, bool value) =>
            new FactPredicate(FactNamespace.World, storyId, "spine_seen", ComparisonOp.Eq, FactValue.FromBool(value));

        private static StoryTemplateData Spine(string id, params FactPredicate[] preconditions)
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            return new StoryTemplateData(id, slots, preconditions ?? Array.Empty<FactPredicate>(), null,
                new[] { "spine" }, "", true, weight: 10);
        }

        private static StoryTemplateData Ordinary(string id)
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            return new StoryTemplateData(id, slots, Array.Empty<FactPredicate>(), null,
                new[] { "bandit" }, "", false, weight: 10);
        }

        private static NpcArchetypeData Archetype(string id, params string[] tags) =>
            new NpcArchetypeData(id, new[] { "Name" }, "faction", 0, tags);

        private static RunPacingSettings Settings(int windowSize = 4, int cap = 2) =>
            new RunPacingSettings(windowSize, lookAheadWindows: 1, maxLiveThreads: 3,
                defaultThreadLifespanWindows: 3, maxSpineRevealsPerRun: cap);

        /// <summary>A fresh planner = a fresh run: run-scoped ledgers start empty; only the fact
        /// store (where the meta bootstrap lands) carries anything across.</summary>
        private RunWindowPlanner FreshRun(RunPacingSettings settings, IReadOnlyList<StoryTemplateData> stories,
            ulong seed = 7, WorldContentDensitySettings density = null)
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
            var threads = new ThreadLedger();
            var storyRun = new StoryRunLedger();
            var definitions = new ThreadCatalog(null, settings.DefaultThreadLifespanWindows);
            var maintenance = new ThreadMaintenanceService(threads, definitions, _evaluator, _logger);
            var archetypes = new[] { Archetype("a", "spine") };
            return new RunWindowPlanner(stories, archetypes, _evaluator, actorFactory, liveActors, random,
                settings, allocator, threads, storyRun, definitions, maintenance, catalog, _logger);
        }

        private static int SpineCount(WindowPlan plan) =>
            plan.Platforms.Count(p => p.Kind == PlannedPlatformKind.Story && p.Story.IsSpine);

        private static string[] PlacedSpineIds(RunWindowPlanner planner, FactStore store, int windows)
        {
            var placed = new List<string>();
            for (int window = 0; window < windows; window++)
            {
                placed.AddRange(planner.PlanWindow(window, store).Platforms
                    .Where(p => p.Kind == PlannedPlatformKind.Story && p.Story.IsSpine)
                    .Select(p => p.Story.StoryId));
            }

            return placed.ToArray();
        }

        [Test]
        public void SeenBeat_IsNeverRePlaced_InALaterRun()
        {
            // Run N saw the beat; the meta bootstrap restored its seen flag. A fresh run's ledgers
            // are empty, so only the cursor can keep it out — and it must, forever.
            _store.SetBool(WorldFacts.SpineSeen, true, "spine_a");
            var planner = FreshRun(Settings(), new[] { Spine("spine_a"), Spine("spine_b") });

            CollectionAssert.AreEqual(new[] { "spine_b" }, PlacedSpineIds(planner, _store, windows: 4),
                "Only the never-seen beat may place; the seen one is gone for good.");
        }

        [Test]
        public void PlacedButNeverSeen_ReturnsToThePool_NextRun()
        {
            // Run 1 places the beat but the player dies before entering it: no seen fact is written.
            var runOne = FreshRun(Settings(cap: 1), new[] { Spine("spine_a") });
            CollectionAssert.AreEqual(new[] { "spine_a" }, PlacedSpineIds(runOne, _store, windows: 2),
                "Run 1 places the beat (spent for THAT run via its ledger).");

            // Run 2 (fresh ledgers, no spine_seen in the meta store): the reveal was never delivered,
            // so it must come back — seen, not placed, is the cross-run rule (PO decision).
            var runTwo = FreshRun(Settings(cap: 1), new[] { Spine("spine_a") });
            CollectionAssert.AreEqual(new[] { "spine_a" }, PlacedSpineIds(runTwo, _store, windows: 2),
                "An undelivered reveal is not lost to death.");
        }

        [Test]
        public void SeenBeats_FromPastRuns_DoNotSpendTheNewRunsCap()
        {
            // Two beats seen across earlier runs; cap 1. The fresh run's cap counts only its own
            // ledger, so the remaining beat still gets this run's single reveal.
            _store.SetBool(WorldFacts.SpineSeen, true, "spine_a");
            _store.SetBool(WorldFacts.SpineSeen, true, "spine_b");
            var planner = FreshRun(Settings(cap: 1), new[] { Spine("spine_a"), Spine("spine_b"), Spine("spine_c") });

            CollectionAssert.AreEqual(new[] { "spine_c" }, PlacedSpineIds(planner, _store, windows: 3));
        }

        [Test]
        public void EchoVariant_GatesOnItsMetaFlag()
        {
            // The mirror-lore echo is an ordinary spine beat gated on a persisted key choice.
            var echo = Spine("spine_echo_conquest", WorldFlagIs("raider_pact_sworn", true));

            var withoutHistory = FreshRun(Settings(), new[] { echo });
            Assert.AreEqual(0, PlacedSpineIds(withoutHistory, _store, windows: 3).Length,
                "No persisted deed - no echo.");

            _store.SetBool(new FactKeyRef(FactNamespace.World, FactScope.Global, "raider_pact_sworn", FactValueType.Bool), true);
            var withHistory = FreshRun(Settings(), new[] { echo });
            CollectionAssert.AreEqual(new[] { "spine_echo_conquest" }, PlacedSpineIds(withHistory, _store, windows: 3),
                "A conquest-leaning history unlocks the conquest echo.");
        }

        [Test]
        public void SiblingSeen_ExcludesTheOtherEchoVariant()
        {
            // Both path flags hold (the player did both deeds over many runs), but the conquest echo
            // was already seen: its own cursor entry excludes it, and the alliance variant's authored
            // sibling gate (spine_seen(conquest) == false) excludes that one - one echo, ever.
            var conquest = Spine("spine_echo_conquest",
                WorldFlagIs("raider_pact_sworn", true), SpineSeenIs("spine_echo_alliance", false));
            var alliance = Spine("spine_echo_alliance",
                WorldFlagIs("barn_bounty_honored", true), SpineSeenIs("spine_echo_conquest", false));
            _store.SetBool(new FactKeyRef(FactNamespace.World, FactScope.Global, "raider_pact_sworn", FactValueType.Bool), true);
            _store.SetBool(new FactKeyRef(FactNamespace.World, FactScope.Global, "barn_bounty_honored", FactValueType.Bool), true);
            _store.SetBool(WorldFacts.SpineSeen, true, "spine_echo_conquest");

            var planner = FreshRun(Settings(), new[] { conquest, alliance });

            Assert.AreEqual(0, PlacedSpineIds(planner, _store, windows: 4).Length,
                "Seeing one variant of the echo pair spends the reveal for both.");
        }

        [Test]
        public void MustHaveSeen_HoldsADeeperBeatUntilItsPredecessor()
        {
            // "Deepens across runs": the cauldron-memory aside authors spine_seen(hint) == true, so
            // it cannot surface before the hint has actually been delivered.
            var memory = Spine("spine_cauldron_memory", SpineSeenIs("spine_cauldron_hint", true));

            var beforeHint = FreshRun(Settings(), new[] { memory });
            Assert.AreEqual(0, PlacedSpineIds(beforeHint, _store, windows: 3).Length);

            _store.SetBool(WorldFacts.SpineSeen, true, "spine_cauldron_hint");
            var afterHint = FreshRun(Settings(), new[] { memory });
            CollectionAssert.AreEqual(new[] { "spine_cauldron_memory" }, PlacedSpineIds(afterHint, _store, windows: 3));
        }

        [Test]
        public void SameSeedAndSameMeta_ProduceIdenticalPlans()
        {
            // D21: the cursor filter and the meta gates are pure pool filters before the seeded pick.
            _store.SetBool(WorldFacts.SpineSeen, true, "spine_a");
            _store.SetBool(new FactKeyRef(FactNamespace.World, FactScope.Global, "raider_pact_sworn", FactValueType.Bool), true);
            var stories = new[]
            {
                Spine("spine_a"), Spine("spine_b"),
                Spine("spine_echo_conquest", WorldFlagIs("raider_pact_sworn", true)),
                Ordinary("s1"), Ordinary("s2")
            };
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 2,
                minPlatformsBetweenQuests: 1, emptyWeight: 1, lootWeight: 1, combatWeight: 0);

            var plannerA = FreshRun(Settings(windowSize: 6), stories, seed: 42, density: density);
            var plannerB = FreshRun(Settings(windowSize: 6), stories, seed: 42, density: density);

            for (int window = 0; window < 3; window++)
            {
                var sigA = plannerA.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                var sigB = plannerB.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                CollectionAssert.AreEqual(sigA, sigB, $"Window {window} diverged for the same seed + memory.");
            }
        }

        [Test]
        public void EmptyMetaStore_NoEchoes_AndTheRunStillPlans()
        {
            // Graceful degrade (FR8): with no memory at all the meta-gated beats simply stay out of
            // the pool - and the rest of the plan is bit-identical to a build without them.
            var withEchoes = new[]
            {
                Spine("spine_echo_conquest", WorldFlagIs("raider_pact_sworn", true)),
                Spine("spine_echo_alliance", WorldFlagIs("barn_bounty_honored", true)),
                Ordinary("s1"), Ordinary("s2")
            };
            var withoutEchoes = new[] { Ordinary("s1"), Ordinary("s2") };
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 2,
                minPlatformsBetweenQuests: 1, emptyWeight: 1, lootWeight: 1, combatWeight: 0);

            var plannerA = FreshRun(Settings(windowSize: 6), withEchoes, seed: 11, density: density);
            var plannerB = FreshRun(Settings(windowSize: 6), withoutEchoes, seed: 11, density: density);

            for (int window = 0; window < 3; window++)
            {
                var sigA = plannerA.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                var sigB = plannerB.PlanWindow(window, _store).Platforms
                    .Select(p => $"{p.Kind}:{p.Story?.StoryId}").ToArray();
                CollectionAssert.AreEqual(sigA, sigB, $"Window {window}: no memory must mean no echoes, nothing else.");
            }
        }
    }
}
