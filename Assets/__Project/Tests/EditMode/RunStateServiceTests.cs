using System;
using System.Collections.Generic;
using CharacterProgression.Core;
using Core.Persistence;
using Inventory.Core;
using Mutation.Core;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using Narrative.Runtime.Snapshots;
using Narrative.Stories.Core;
using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// P2-2 run-state aggregate: the whole-run capture/restore round trip across fresh service
    /// instances (FR4/FR5), the crafting capture-time normalization (D10), id-counter continuity,
    /// and the W3-1 savepoint guard at the aggregate level.
    /// </summary>
    [TestFixture]
    public class RunStateServiceTests
    {
        private sealed class FakeBlankData : IPartBlankDataSource
        {
            private readonly Dictionary<string, PartBlankData> _blanks = new Dictionary<string, PartBlankData>();

            public FakeBlankData Add(string definitionId, int socketCount)
            {
                _blanks[definitionId] = new PartBlankData(definitionId, definitionId, "slot", "", socketCount);
                return this;
            }

            public IReadOnlyList<PartBlankData> All => new List<PartBlankData>(_blanks.Values);
            public bool TryGet(string definitionId, out PartBlankData blank) =>
                _blanks.TryGetValue(definitionId ?? string.Empty, out blank);
        }

        private sealed class FakeFusionResolver : IFusionResolver
        {
            public FusionResult Resolve(IReadOnlyList<string> inputDefinitionIds) =>
                new FusionResult("fused", isSignature: false);
        }

        /// <summary>One full set of run services, as a fresh scene container would build them.</summary>
        private sealed class RunWorld
        {
            public readonly FactStore Facts = new FactStore();
            public readonly DeterministicRandom Random = new DeterministicRandom(99);
            public readonly ThreadLedger Threads = new ThreadLedger();
            public readonly StoryRunLedger Stories = new StoryRunLedger();
            public readonly LiveActorRegistry Actors = new LiveActorRegistry();
            public readonly LiveQuestRegistry Quests = new LiveQuestRegistry();
            public readonly RunProgressionRecord Progression = new RunProgressionRecord();
            public readonly InventoryModel Inventory = new InventoryModel();
            public readonly PartInventoryModel PartStash = new PartInventoryModel();
            public readonly BlankRack Rack = new BlankRack(capacity: 3);
            public readonly SocketingModel Socketing;
            public readonly CraftingSession Crafting;
            public readonly IFragmentLibrary Fragments;
            public readonly NarrativeSaveService NarrativeSave;
            public readonly Loot.Core.RunSeedProvider Seed = new Loot.Core.RunSeedProvider();

            public RunWorld()
            {
                Seed.SetSeed(4242);
                var blankData = new FakeBlankData().Add("blank_leg", socketCount: 2);
                Socketing = new SocketingModel(Inventory, Rack, blankData);
                Crafting = new CraftingSession(new FakeFusionResolver(), Inventory, itemsToCombine: 3);
                Fragments = new FragmentLibrary(
                    new[] { Dialogue("dlg_bounty") },
                    new[] { BountyQuest() },
                    Array.Empty<EnemyFragment>());
                NarrativeSave = new NarrativeSaveService(Facts, Random, seed: 99,
                    threads: Threads, stories: Stories);
            }

            public RunStateService Service() => new RunStateService(
                NarrativeSave, Actors, Quests, Fragments, Progression, Progression,
                Inventory, PartStash, Rack, Socketing, Crafting, Seed);

            public RunStateService Service(RunStartConditions startConditions) => new RunStateService(
                NarrativeSave, Actors, Quests, Fragments, Progression, Progression,
                Inventory, PartStash, Rack, Socketing, Crafting, Seed,
                startConditions: startConditions);
        }

        private static DialogueData Dialogue(string id) =>
            new DialogueData(id, "{}", "start", Array.Empty<string>(), null, Array.Empty<string>());

        private static QuestData BountyQuest()
        {
            var objective = new QuestObjective("obj_grain", "", QuestObjectiveKind.Reach, 2, null);
            return new QuestData("qst_bounty", "Bounty", "", new[] { objective },
                Array.Empty<string>(), null, null);
        }

        [Test]
        public void Capture_StampsTheStartingBiome_FromRunStartConditions_O1()
        {
            var world = new RunWorld();
            var service = world.Service(new RunStartConditions(string.Empty, "Desert"));

            Assert.IsTrue(service.TryCaptureAll(DialogueRunnerState.Ended, out var snapshot));
            Assert.AreEqual("Desert", snapshot.StartingBiome,
                "the Hub's entry choice must ride every savepoint");
        }

        [Test]
        public void Capture_WithoutStartConditions_LeavesStartingBiomeEmpty()
        {
            var world = new RunWorld();

            Assert.IsTrue(world.Service().TryCaptureAll(DialogueRunnerState.Ended, out var snapshot));
            Assert.AreEqual(string.Empty, snapshot.StartingBiome);
        }

        [Test]
        public void FullAggregate_RoundTripsAcrossFreshInstances()
        {
            var runOne = new RunWorld();

            // A lived-in run: facts, threads, an actor with per-actor state, a half-done quest,
            // items, a racked blank with one socketed artifact, a shed part, progression extras.
            runOne.Facts.Set(new FactKey(FactNamespace.World, "", "pass_cleared"), FactValue.FromBool(true));
            runOne.Threads.Open("thread_bounty", ThreadKind.Ephemeral, 0);
            runOne.Actors.Register(new NpcInstance("npc_1", "farmer", "Old Miro", "villagers"));
            runOne.Facts.Set(new FactKey(FactNamespace.Actor, "npc_1", "met"), FactValue.FromBool(true));

            var quest = new QuestInstance(BountyQuest(), runOne.Progression);
            quest.Start();
            quest.AdvanceObjective("obj_grain");
            runOne.Quests.Register(quest);

            var sword = runOne.Inventory.Add("sword");
            var gem = runOne.Inventory.Add("gem");
            runOne.PartStash.Add("part_old_leg");
            Assert.IsTrue(runOne.Rack.TryAdd("blank_leg", out var blank));
            Assert.IsTrue(runOne.Socketing.TrySocket(blank.InstanceId, gem.InstanceId));
            runOne.Progression.RecordNpcEncounter("npc_1");
            runOne.Progression.RecordChoice("spared_raider", "yes");
            runOne.Random.NextInt(100);

            Assert.IsTrue(runOne.Service().TryCaptureAll(DialogueRunnerState.Ended, out var snapshot));
            Assert.AreEqual(4242, snapshot.RunSeed,
                "the ROOT run seed must ride the save — platform geometry is a pure function of it");
            int expectedNextDraw = new DeterministicRandom(0) { State = runOne.Random.State }.NextInt(100);

            // "Relaunch": everything rebuilt from scratch, then restored.
            var runTwo = new RunWorld();
            runTwo.Service().RestoreAll(snapshot);

            Assert.IsTrue(runTwo.Facts.GetOrDefault(
                new FactKey(FactNamespace.World, "", "pass_cleared"), FactValue.FromBool(false)).AsBool());
            Assert.IsTrue(runTwo.Threads.TryGet("thread_bounty", out _));
            Assert.AreEqual(1, runTwo.Actors.LiveActors.Count);
            Assert.AreEqual("Old Miro", runTwo.Actors.LiveActors[0].ChosenDisplayName);
            Assert.IsTrue(runTwo.Facts.GetOrDefault(
                new FactKey(FactNamespace.Actor, "npc_1", "met"), FactValue.FromBool(false)).AsBool());

            Assert.IsTrue(runTwo.Quests.TryGet("qst_bounty", out var restoredQuest));
            Assert.AreEqual(QuestState.Active, restoredQuest.State);
            Assert.AreEqual(1, restoredQuest.ProgressOf("obj_grain"));
            Assert.IsFalse(restoredQuest.IsObjectiveComplete("obj_grain"));
            Assert.IsTrue(runTwo.Progression.IsQuestActive("qst_bounty"), "recorder bridge repopulated");

            Assert.IsTrue(runTwo.Inventory.TryGet(sword.InstanceId, out var restoredSword));
            Assert.AreEqual("sword", restoredSword.DefinitionId);
            CollectionAssert.AreEqual(new[] { "part_old_leg" }, new List<string>(runTwo.PartStash.PartIds));
            Assert.AreEqual(1, runTwo.Rack.Blanks.Count);
            Assert.AreEqual(blank.InstanceId, runTwo.Rack.Blanks[0].InstanceId);
            var socketed = runTwo.Socketing.SocketedArtifacts(blank.InstanceId);
            Assert.AreEqual(1, socketed.Count, "socketed artifact re-socketed, not lost");
            Assert.AreEqual(gem.InstanceId, socketed[0].InstanceId);
            Assert.IsFalse(runTwo.Inventory.TryGet(gem.InstanceId, out _), "socketed artifact is not also in the inventory");

            Assert.IsTrue(runTwo.Progression.HasEncounteredNpc("npc_1"));
            Assert.IsTrue(runTwo.Progression.TryGetChoice("spared_raider", out var choice));
            Assert.AreEqual("yes", choice);

            Assert.AreEqual(expectedNextDraw, runTwo.Random.NextInt(100), "post-restore draws replay identically (FR12)");
        }

        [Test]
        public void IdCounters_SurviveRestore_SoNewIdsStayUnique()
        {
            var runOne = new RunWorld();
            var a = runOne.Inventory.Add("sword");
            var b = runOne.Inventory.Add("gem");
            runOne.Inventory.Remove(a.InstanceId); // freed slot must NOT be re-minted after restore
            Assert.IsTrue(runOne.Rack.TryAdd("blank_leg", out var blank));

            Assert.IsTrue(runOne.Service().TryCaptureAll(DialogueRunnerState.Ended, out var snapshot));

            var runTwo = new RunWorld();
            runTwo.Service().RestoreAll(snapshot);

            var fresh = runTwo.Inventory.Add("hide");
            Assert.AreNotEqual(a.InstanceId, fresh.InstanceId);
            Assert.AreNotEqual(b.InstanceId, fresh.InstanceId);
            Assert.IsTrue(runTwo.Rack.TryAdd("blank_leg", out var freshBlank));
            Assert.AreNotEqual(blank.InstanceId, freshBlank.InstanceId);
        }

        [Test]
        public void MidStagingCrafting_IsNormalizedToInventory_WithoutTouchingTheLiveSession()
        {
            var runOne = new RunWorld();
            var a = runOne.Inventory.Add("sword");
            var b = runOne.Inventory.Add("gem");
            Assert.IsTrue(runOne.Crafting.TrySelect(a.InstanceId));
            Assert.IsTrue(runOne.Crafting.TrySelect(b.InstanceId));
            Assert.AreEqual(CraftingState.Selecting, runOne.Crafting.State);

            Assert.IsTrue(runOne.Service().TryCaptureAll(DialogueRunnerState.Ended, out var snapshot));

            // A3: the invisible autosave must not mutate live play.
            Assert.AreEqual(CraftingState.Selecting, runOne.Crafting.State);
            Assert.AreEqual(2, runOne.Crafting.StagedItems.Count);
            Assert.AreEqual(0, runOne.Inventory.Items.Count);

            // FR7: on continue the items are back in the stash and the session re-begins Idle.
            var runTwo = new RunWorld();
            runTwo.Service().RestoreAll(snapshot);
            Assert.AreEqual(CraftingState.Idle, runTwo.Crafting.State);
            Assert.AreEqual(2, runTwo.Inventory.Items.Count);
            Assert.IsTrue(runTwo.Inventory.TryGet(a.InstanceId, out _));
            Assert.IsTrue(runTwo.Inventory.TryGet(b.InstanceId, out _));
        }

        [Test]
        public void CompletedQuest_RestoresTerminalStateAndRewardGuard()
        {
            var runOne = new RunWorld();
            var quest = new QuestInstance(BountyQuest(), runOne.Progression);
            quest.Start();
            quest.AdvanceObjective("obj_grain", 2);
            quest.Complete();
            quest.MarkRewardsGranted();
            runOne.Quests.Register(quest);

            Assert.IsTrue(runOne.Service().TryCaptureAll(DialogueRunnerState.Ended, out var snapshot));

            var runTwo = new RunWorld();
            runTwo.Service().RestoreAll(snapshot);

            Assert.IsTrue(runTwo.Quests.TryGet("qst_bounty", out var restored));
            Assert.AreEqual(QuestState.Completed, restored.State);
            Assert.IsTrue(restored.IsObjectiveComplete("obj_grain"));
            Assert.IsTrue(restored.RewardsGranted, "rewards must not re-grant after continue");
            Assert.IsTrue(runTwo.Progression.IsQuestCompleted("qst_bounty"));
        }

        [Test]
        public void AwaitingExternalDialogue_BlocksAggregateCapture_W3_1()
        {
            var world = new RunWorld();
            Assert.IsFalse(world.Service().TryCaptureAll(DialogueRunnerState.AwaitingExternal, out var refused));
            Assert.IsNull(refused);
        }

        [Test]
        public void CastingSnapshot_RestoresById_WithCanonicalContext()
        {
            var world = new RunWorld();
            var actor = new NpcInstance("npc_1", "farmer", "Old Miro", "villagers");
            world.Actors.Register(actor);

            var original = new Casting(actor, Dialogue("dlg_bounty"), BountyQuest(), "wolf",
                new ContextBag().BindSubject("$self", actor.InstanceId).BindSubject("$faction", actor.FactionId),
                "story_bounty", "thread_bounty");

            var snapshot = CastingSnapshotMapper.Capture(original);
            var restored = CastingSnapshotMapper.Restore(snapshot, world.Actors, world.Fragments);

            Assert.IsNotNull(restored);
            Assert.AreSame(actor, restored.Actor, "actor resolves to the registry instance, not a copy");
            Assert.AreEqual("dlg_bounty", restored.Dialogue.DialogueId);
            Assert.AreEqual("qst_bounty", restored.OptionalQuest.QuestId);
            Assert.AreEqual("wolf", restored.OptionalEnemyId);
            Assert.AreEqual("story_bounty", restored.StoryId);
            Assert.AreEqual("thread_bounty", restored.ThreadId);
            Assert.IsTrue(restored.Context.TryGet("$self", out var self));
            Assert.AreEqual("npc_1", self);
            Assert.IsTrue(restored.Context.TryGetVariable("quest_available", out var questVar));
            Assert.AreEqual(true, questVar);
        }

        [Test]
        public void CastingSnapshot_MissingFragment_RestoresNullNotCrash()
        {
            var world = new RunWorld();
            var snapshot = new CastingSnapshot { ActorInstanceId = "ghost", DialogueId = "gone" };
            Assert.IsNull(CastingSnapshotMapper.Restore(snapshot, world.Actors, world.Fragments));
        }
    }
}
