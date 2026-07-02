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
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunWindowPlannerTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeThemeProvider : ICurrentThemeProvider
        {
            private LevelTheme _theme = LevelTheme.Forest;
            public LevelTheme CurrentTheme => _theme;
            public void SetTheme(LevelTheme theme) => _theme = theme;
        }

        /// <summary>Every slot with an eligible story becomes a quest slot (avg 1, no spacing) and
        /// ambient slots stay empty — reproducing the pre-density "fill with stories, pad empty"
        /// behavior most eligibility/recasting tests were written against.</summary>
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
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.Actor, "looted_barn", FactScope.PerActor, FactValueType.Bool, FactValue.FromBool(false)),
                // Barn-demo partition facts (window-1 choices that route window-2 selection).
                new FactKeyInfo(FactNamespace.World, "barn_raided", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "barn_quest_offered", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "barn_quest_accepted", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "grain_recovered", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                // Cross-actor fork + frog_marsh passport facts (deeper demo web).
                new FactKeyInfo(FactNamespace.World, "raider_offer_taken", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "reads_as_frogfolk", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "frog_quest_offered", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "frog_quest_accepted", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false))
            });
            _store = new FactStore(registry, _logger);
            var resolver = new SubjectResolver(_logger);
            _evaluator = new PreconditionEvaluator(resolver, registry, _logger);
        }

        private static FactPredicate PassCleared(bool value) =>
            new FactPredicate(FactNamespace.World, "", "pass_cleared", ComparisonOp.Eq, FactValue.FromBool(value));

        // Actor-scoped precondition resolved against the cast actor's $self subject (D16).
        private static FactPredicate LootedBarn(bool value) =>
            new FactPredicate(FactNamespace.Actor, "$self", "looted_barn", ComparisonOp.Eq, FactValue.FromBool(value));

        // A global world-fact equality predicate (the barn-demo gates are all world bools).
        private static FactPredicate World(string key, bool value) =>
            new FactPredicate(FactNamespace.World, "", key, ComparisonOp.Eq, FactValue.FromBool(value));

        // A barn-thread story with one dialogue slot and an AND-composed precondition list (R10).
        private static StoryTemplateData StoryAnd(string id, string[] tags, params FactPredicate[] preconditions)
        {
            var slots = new List<StorySlot> { new StorySlot("d", SlotKind.Dialogue, new[] { "talk" }, false) };
            return new StoryTemplateData(id, slots, preconditions, null, tags, "barn_raid", false, weight: 10);
        }

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
            IReadOnlyList<NpcArchetypeData> archetypes, ulong seed = 7,
            WorldContentDensitySettings density = null, IBiomeMonsterPoolCatalog monsterPools = null)
        {
            var random = new DeterministicRandom(seed);
            var actorFactory = new ActorInstanceFactory(random);
            // The registry lives with the planner so a minted actor stays queryable across windows (D11).
            var liveActors = new LiveActorRegistry();
            // The allocator shares the planner's seeded stream and carries quest spacing across windows.
            var allocator = new WorldContentAllocator(density ?? QuestEverywhere,
                monsterPools ?? new BiomeMonsterPoolCatalog(null), new FakeThemeProvider(), random, _logger);
            return new RunWindowPlanner(stories, archetypes, _evaluator, actorFactory, liveActors, random,
                settings, allocator, _logger);
        }

        private static int StoryCount(WindowPlan plan) =>
            plan.Platforms.Count(p => p.Kind == PlannedPlatformKind.Story);

        [Test]
        public void PlanIsAlwaysPaddedToWindowSize()
        {
            var settings = new RunPacingSettings(windowSize: 5, lookAheadWindows: 1);
            var plan = Planner(settings, new[] { Story("s1", 20, new[] { "bandit" }) },
                new[] { Archetype("a", "bandit") }).PlanWindow(0, _store);

            Assert.AreEqual(5, plan.Platforms.Count);     // padded to window size
            Assert.AreEqual(1, StoryCount(plan));         // one eligible story, rest empty/ambient
        }

        [Test]
        public void AmbientMix_EmitsLootAndCombatKinds_FromDensityWeights()
        {
            // No stories at all: every slot is an ambient draw. Loot and combat kinds must appear per the
            // configured weights, and combat platforms carry the biome-pool enemy id.
            var settings = new RunPacingSettings(windowSize: 12, lookAheadWindows: 1);
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 100,
                minPlatformsBetweenQuests: 0, emptyWeight: 0, lootWeight: 1, combatWeight: 1);
            var pool = new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<int>>
            {
                { LevelTheme.Forest, new[] { 7 } }
            });

            var plan = Planner(settings, Array.Empty<StoryTemplateData>(), Array.Empty<NpcArchetypeData>(),
                density: density, monsterPools: pool).PlanWindow(0, _store);

            Assert.AreEqual(12, plan.Platforms.Count);
            Assert.IsTrue(plan.Platforms.Any(p => p.Kind == PlannedPlatformKind.Loot));
            var combats = plan.Platforms.Where(p => p.Kind == PlannedPlatformKind.Combat).ToList();
            Assert.IsTrue(combats.Count > 0);
            foreach (var combat in combats)
            {
                Assert.AreEqual(7, combat.EnemyId);   // from the Forest pool
                Assert.IsTrue(combat.IsCombat);
                Assert.IsNull(combat.Story);          // ambient: no story, no dialogue
            }
        }

        [Test]
        public void QuestSpacing_HeldAcrossConsecutiveWindows()
        {
            // The spacing counter is allocator state, not window state: with min spacing 2 no two story
            // platforms may sit closer than 3 slots apart across the whole run, window boundaries included.
            const int spacing = 2;
            var settings = new RunPacingSettings(windowSize: 3, lookAheadWindows: 1);
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 1,
                minPlatformsBetweenQuests: spacing, emptyWeight: 1, lootWeight: 0, combatWeight: 0);
            var stories = Enumerable.Range(1, 6).Select(i => Story($"s{i}", 10, new[] { "bandit" })).ToArray();
            var planner = Planner(settings, stories, new[] { Archetype("a", "bandit") }, density: density);

            var questSlots = new List<int>();
            for (int window = 0; window < 4; window++)
            {
                var plan = planner.PlanWindow(window, _store);
                for (int i = 0; i < plan.Platforms.Count; i++)
                {
                    if (plan.Platforms[i].Kind == PlannedPlatformKind.Story)
                    {
                        questSlots.Add(window * 3 + i);
                    }
                }
            }

            Assert.GreaterOrEqual(questSlots.Count, 2, "Expected at least two quests over four windows.");
            for (int i = 1; i < questSlots.Count; i++)
            {
                Assert.Greater(questSlots[i] - questSlots[i - 1], spacing,
                    $"Quests at run slots {questSlots[i - 1]} and {questSlots[i]} violate the min spacing.");
            }
        }

        [Test]
        public void SameSeed_TwoWindowPlansIdentical_AcrossAllKinds()
        {
            var settings = new RunPacingSettings(windowSize: 6, lookAheadWindows: 1);
            var density = new WorldContentDensitySettings(averagePlatformsPerQuest: 2,
                minPlatformsBetweenQuests: 1, emptyWeight: 1, lootWeight: 1, combatWeight: 1);
            var pool = new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<int>>
            {
                { LevelTheme.Forest, new[] { 5, 9 } }
            });
            var stories = Enumerable.Range(1, 4).Select(i => Story($"s{i}", 10, new[] { "bandit" })).ToArray();
            var archetypes = new[] { Archetype("a", "bandit") };

            var plannerA = Planner(settings, stories, archetypes, seed: 42, density: density, monsterPools: pool);
            var plannerB = Planner(settings, stories, archetypes, seed: 42, density: density, monsterPools: pool);

            for (int window = 0; window < 2; window++)
            {
                var planA = plannerA.PlanWindow(window, _store);
                var planB = plannerB.PlanWindow(window, _store);
                var sigA = planA.Platforms.Select(p => $"{p.Kind}:{p.Story?.StoryId}:{p.EnemyId}").ToArray();
                var sigB = planB.Platforms.Select(p => $"{p.Kind}:{p.Story?.StoryId}:{p.EnemyId}").ToArray();
                CollectionAssert.AreEqual(sigA, sigB, $"Window {window} diverged for the same seed.");
            }
        }

        [Test]
        public void SameSeedAndFacts_ProduceIdenticalPlan()
        {
            var settings = new RunPacingSettings(windowSize: 4, lookAheadWindows: 1);
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
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
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
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var ghostStory = Story("ghost", 10, new[] { "ghost" }); // no archetype carries "ghost"
            var plan = Planner(settings, new[] { ghostStory }, new[] { Archetype("a", "bandit") }).PlanWindow(0, _store);

            Assert.AreEqual(1, StoryCount(plan));
        }

        [Test]
        public void NoArchetypesAtAll_PlacesNothing()
        {
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var plan = Planner(settings, new[] { Story("s1", 10, new[] { "bandit" }) },
                Array.Empty<NpcArchetypeData>()).PlanWindow(0, _store);

            Assert.AreEqual(0, StoryCount(plan));
        }

        [Test]
        public void OverlappingArchetypePreferredOverNonMatching()
        {
            var settings = new RunPacingSettings(windowSize: 1, lookAheadWindows: 1);
            var archetypes = new[] { Archetype("arch_match", "bandit"), Archetype("arch_other", "beast") };
            var plan = Planner(settings, new[] { Story("s1", 10, new[] { "bandit" }) }, archetypes).PlanWindow(0, _store);

            var story = plan.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.AreEqual("arch_match", story.Actor.ArchetypeId);
        }

        [Test]
        public void PlacedStory_GetsAnAssignedActor()
        {
            var settings = new RunPacingSettings(windowSize: 1, lookAheadWindows: 1);
            var plan = Planner(settings, new[] { Story("s1", 10, new[] { "bandit" }) },
                new[] { Archetype("arch_bandit", "bandit") }).PlanWindow(0, _store);

            var story = plan.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.IsNotNull(story.Actor);
            Assert.AreEqual("arch_bandit", story.Actor.ArchetypeId);
        }

        [Test]
        public void RecurringActor_RecastIntoMotiveStory_ByActorScopedFact()
        {
            // The raider-arc acceptance scenario (D11 + D16): window N places the barn story and mints a
            // raider; resolving it writes actor.<raiderId>.looted_barn. Window N+1 a "raider motive" story
            // becomes eligible *by that actor-scoped fact* and the *same* raider instance is recast.
            var settings = new RunPacingSettings(windowSize: 1, lookAheadWindows: 1);
            var barn = Story("barn", 10, new[] { "raider" }, precondition: PassCleared(false));
            // "motive" tags overlap no archetype: a placement here proves the actor pin overrides the P1 tag preference.
            var motive = Story("motive", 10, new[] { "motive" }, precondition: LootedBarn(true));
            var planner = Planner(settings, new[] { barn, motive }, new[] { Archetype("arch_raider", "raider") });

            var windowN = planner.PlanWindow(0, _store);
            var barnPlatform = windowN.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.AreEqual("barn", barnPlatform.Story.StoryId); // only the world-gated barn is eligible at first
            var raider = barnPlatform.Actor;

            // Resolving the barn writes the raider's actor-scoped fact; the world gate also flips closed.
            _store.Set(new FactKey(FactNamespace.Actor, raider.InstanceId, "looted_barn"), FactValue.FromBool(true));
            _store.Set(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(true));

            var windowNext = planner.PlanWindow(1, _store);
            var placed = windowNext.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.AreEqual("motive", placed.Story.StoryId);            // eligible by the actor-scoped fact (item 1)
            Assert.AreEqual(raider.InstanceId, placed.Actor.InstanceId); // same instance recast (item 2)
        }

        [Test]
        public void BarnDemo_Window2SelectionPartitions_ByWindow1Choices()
        {
            // D5/D15 + D11/D16 acceptance test: window 1 places the victim and the raider; the player's two
            // choices write facts that make EXACTLY ONE of three window-2 reactions eligible. The three
            // preconditions partition the outcome space - grain_recovered (A/C) and looted_barn (B) are
            // mutually exclusive by the raider choice - so every play resolves to a single story. For the
            // let-go combos the recast raider must be the *same* NpcInstance from window 1 (recurring actor).
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var archetypes = new[]
            {
                Archetype("arch_villager", "villager", "farmer"),
                Archetype("arch_raider", "raider")
            };

            var combos = new[]
            {
                (accepted: true,  fought: true,  expected: "story_grateful_farmer",  recurring: false),
                (accepted: false, fought: true,  expected: "story_starving_village", recurring: false),
                (accepted: true,  fought: false, expected: "story_raider_motive",    recurring: true),
                (accepted: false, fought: false, expected: "story_raider_motive",    recurring: true)
            };

            foreach (var combo in combos)
            {
                // Fresh store + planner per play so live actors and facts never leak between combos.
                SetUp();
                var victim = StoryAnd("story_barn_victim", new[] { "villager", "barn" }, World("barn_quest_offered", false));
                var raid = StoryAnd("story_barn_raid", new[] { "raider", "barn" }, World("barn_raided", false));
                var grateful = StoryAnd("story_grateful_farmer", new[] { "villager", "barn" },
                    World("barn_quest_accepted", true), World("grain_recovered", true));
                var motive = StoryAnd("story_raider_motive", new[] { "motive" }, LootedBarn(true));
                var starving = StoryAnd("story_starving_village", new[] { "villager", "barn" },
                    World("barn_quest_accepted", false), World("grain_recovered", true));
                var planner = Planner(settings, new[] { victim, raid, grateful, motive, starving }, archetypes);

                // Window 1: only the two world-gated openers are eligible (A/C need grain, B needs a live
                // looter). Capture the minted raider so we can prove the recast identity later.
                var window1 = planner.PlanWindow(0, _store);
                Assert.AreEqual(2, StoryCount(window1)); // victim + raider co-appear in window 1
                var raider = window1.Platforms
                    .First(p => p.Kind == PlannedPlatformKind.Story && p.Story.StoryId == "story_barn_raid").Actor;

                // Apply the window-1 outcome facts the dialogues would have written for this combo.
                _store.Set(FactKey.Global(FactNamespace.World, "barn_raided"), FactValue.FromBool(true));
                _store.Set(FactKey.Global(FactNamespace.World, "barn_quest_offered"), FactValue.FromBool(true));
                _store.Set(FactKey.Global(FactNamespace.World, "barn_quest_accepted"), FactValue.FromBool(combo.accepted));
                var lootedKey = new FactKey(FactNamespace.Actor, raider.InstanceId, "looted_barn");
                _store.Set(lootedKey, FactValue.FromBool(true)); // the raid always sets it on the raider
                if (combo.fought)
                {
                    // Fight-win returns the grain and clears the raider's at-large flag (routes to A/C).
                    _store.Set(FactKey.Global(FactNamespace.World, "grain_recovered"), FactValue.FromBool(true));
                    _store.Set(lootedKey, FactValue.FromBool(false));
                }
                // Let-go leaves looted_barn true: the raider is still at large and becomes candidate B.

                // Window 2: exactly one reaction is eligible by the partitioned preconditions.
                var window2 = planner.PlanWindow(1, _store);
                Assert.AreEqual(1, StoryCount(window2),
                    $"accepted={combo.accepted} fought={combo.fought} should yield exactly one window-2 story");
                var placed = window2.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
                Assert.AreEqual(combo.expected, placed.Story.StoryId,
                    $"accepted={combo.accepted} fought={combo.fought}");

                if (combo.recurring)
                {
                    Assert.AreEqual(raider.InstanceId, placed.Actor.InstanceId); // same raider recast (B)
                }
            }
        }

        [Test]
        public void ActorScopedStory_WithoutALiveActor_IsIneligible()
        {
            // Item 1 is required: an actor-scoped gate cannot pass under the world-only context the planner
            // used before this change (no live actor exists to satisfy $self), even with world facts set.
            var settings = new RunPacingSettings(windowSize: 1, lookAheadWindows: 1);
            var motive = Story("motive", 10, new[] { "motive" }, precondition: LootedBarn(true));
            _store.Set(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(true));

            var plan = Planner(settings, new[] { motive }, new[] { Archetype("arch_raider", "raider") }).PlanWindow(0, _store);

            Assert.AreEqual(0, StoryCount(plan));
        }

        [Test]
        public void WrongActor_DoesNotSatisfyActorScopedGate()
        {
            // Two raiders are minted; only one looted the barn. The motive recast must pick that one.
            var settings = new RunPacingSettings(windowSize: 2, lookAheadWindows: 1);
            var barnA = Story("barnA", 10, new[] { "raider" }, precondition: PassCleared(false));
            var barnB = Story("barnB", 10, new[] { "raider" }, precondition: PassCleared(false));
            var motive = Story("motive", 10, new[] { "motive" }, precondition: LootedBarn(true));
            var planner = Planner(settings, new[] { barnA, barnB, motive }, new[] { Archetype("arch_raider", "raider") });

            var windowN = planner.PlanWindow(0, _store);
            var actors = windowN.Platforms.Where(p => p.Kind == PlannedPlatformKind.Story).Select(p => p.Actor).ToList();
            Assert.AreEqual(2, actors.Count);
            var looter = actors[1];

            _store.Set(new FactKey(FactNamespace.Actor, looter.InstanceId, "looted_barn"), FactValue.FromBool(true));
            _store.Set(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(true));

            var placed = planner.PlanWindow(1, _store).Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.AreEqual("motive", placed.Story.StoryId);
            Assert.AreEqual(looter.InstanceId, placed.Actor.InstanceId); // the barn-looter, not the other raider
        }

        [Test]
        public void PassportFact_FlipsClosedDoorToOpen()
        {
            // D15/D16 passport gating (frog_marsh thread): two stories on OPPOSITE values of one world fact
            // (reads_as_frogfolk). While false only the closed-door story is eligible; flipping the passport
            // true swaps it for the open-door (quest-offering) story. The passport flip in miniature.
            var settings = new RunPacingSettings(windowSize: 1, lookAheadWindows: 1);
            var closed = Story("frog_closed", 10, new[] { "frogfolk" },
                precondition: World("reads_as_frogfolk", false), thread: "frog_marsh");
            var open = Story("frog_open", 10, new[] { "frogfolk" },
                precondition: World("reads_as_frogfolk", true), thread: "frog_marsh");
            var planner = Planner(settings, new[] { closed, open }, new[] { Archetype("arch_frogfolk", "frogfolk") });

            var before = planner.PlanWindow(0, _store);
            Assert.AreEqual("frog_closed",
                before.Platforms.First(p => p.Kind == PlannedPlatformKind.Story).Story.StoryId);

            _store.Set(FactKey.Global(FactNamespace.World, "reads_as_frogfolk"), FactValue.FromBool(true));
            var after = planner.PlanWindow(1, _store);
            Assert.AreEqual("frog_open",
                after.Platforms.First(p => p.Kind == PlannedPlatformKind.Story).Story.StoryId);
        }

        [Test]
        public void CrossActorFork_RaiderCounterOfferEligible_AfterBountyTaken()
        {
            // The cross-actor moral fork (quest-as-reward.md §4): the player takes the farmer's bounty
            // (world.barn_quest_accepted == true), and the raid leaves the raider at large. A couple
            // platforms later the SAME raider, recast by his actor-scoped fact, becomes eligible to make the
            // mutually-exclusive counter-offer - opposed facts on one shared-actor thread, separated in time.
            var settings = new RunPacingSettings(windowSize: 1, lookAheadWindows: 1);
            var raid = Story("story_barn_raid", 10, new[] { "raider" },
                precondition: World("barn_raided", false), thread: "barn_raid");
            var counter = Story("story_raider_motive", 10, new[] { "motive" },
                precondition: LootedBarn(true), thread: "barn_raid");
            var planner = Planner(settings, new[] { raid, counter }, new[] { Archetype("arch_raider", "raider") });

            var window1 = planner.PlanWindow(0, _store);
            var raidPlatform = window1.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.AreEqual("story_barn_raid", raidPlatform.Story.StoryId); // only the world-gated raid at first
            var raider = raidPlatform.Actor;

            _store.Set(FactKey.Global(FactNamespace.World, "barn_raided"), FactValue.FromBool(true));
            _store.Set(FactKey.Global(FactNamespace.World, "barn_quest_accepted"), FactValue.FromBool(true)); // bounty taken
            _store.Set(new FactKey(FactNamespace.Actor, raider.InstanceId, "looted_barn"), FactValue.FromBool(true));

            var window2 = planner.PlanWindow(1, _store);
            var placed = window2.Platforms.First(p => p.Kind == PlannedPlatformKind.Story);
            Assert.AreEqual("story_raider_motive", placed.Story.StoryId);          // counter-offer eligible
            Assert.AreEqual(raider.InstanceId, placed.Actor.InstanceId);           // same raider recast
        }

        [Test]
        public void TwoThreads_BothEligibleInOneWindow()
        {
            // Multi-thread (D12/D14): a barn_raid opener and a frog_marsh opener are both eligible at once, so
            // a single window holds beats from two DISTINCT threads. Exercises the planner's concurrent-thread
            // selection (the within-window "continue the started thread" preference operates over real threads).
            var settings = new RunPacingSettings(windowSize: 4, lookAheadWindows: 1);
            var barn = Story("story_barn_victim", 10, new[] { "villager" },
                precondition: World("barn_quest_offered", false), thread: "barn_raid");
            var frog = Story("story_frog_elder_closed", 10, new[] { "frogfolk" },
                precondition: World("reads_as_frogfolk", false), thread: "frog_marsh");
            var archetypes = new[] { Archetype("arch_villager", "villager"), Archetype("arch_frogfolk", "frogfolk") };

            var plan = Planner(settings, new[] { barn, frog }, archetypes).PlanWindow(0, _store);

            Assert.AreEqual(2, StoryCount(plan));
            var threads = plan.Platforms
                .Where(p => p.Kind == PlannedPlatformKind.Story)
                .Select(p => p.Story.ThreadId)
                .Distinct()
                .ToList();
            CollectionAssert.AreEquivalent(new[] { "barn_raid", "frog_marsh" }, threads);
        }
    }
}
