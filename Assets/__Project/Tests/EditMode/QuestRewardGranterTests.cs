using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Loot.Application;
using Loot.Core;
using Mutation.Core;
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
    public class QuestRewardGranterTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeInventory : IInventoryModel
        {
            public readonly List<string> Added = new();
            private int _nextId;

            public IReadOnlyList<ArtifactInstance> Items => Array.Empty<ArtifactInstance>();
            public event Action<ArtifactInstance> OnItemAdded;
            public event Action<ArtifactInstance> OnItemRemoved;

            public ArtifactInstance Add(string definitionId)
            {
                Added.Add(definitionId);
                var instance = new ArtifactInstance(_nextId++, definitionId);
                OnItemAdded?.Invoke(instance);
                return instance;
            }

            public void Return(ArtifactInstance instance) { }
            public bool Remove(int instanceId) => false;
            public bool TryGet(int instanceId, out ArtifactInstance instance) { instance = null; return false; }
            public ArtifactInstance CreateDetachedInstance(string definitionId) => new ArtifactInstance(_nextId++, definitionId);
            public int NextInstanceId => _nextId;
            public void RestoreFrom(IReadOnlyList<ArtifactInstance> items, int nextInstanceId) => _nextId = nextInstanceId;
        }

        private sealed class FakeSeedProvider : IRunSeedProvider
        {
            public int RunSeed => 424242;
            public void SetSeed(int seed) { }
        }

        private FakeStoryManager _fake;
        private DialogueRunner _runner;
        private LiveQuestRegistry _quests;
        private FakeInventory _inventory;
        private BlankRack _rack;

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(Array.Empty<FactKeyInfo>());
            var store = new FactStore(registry, logger);
            var applier = new FactEffectApplier(new SubjectResolver(logger), logger);
            var parser = new DialogueTagParser(registry, logger);
            _fake = new FakeStoryManager();
            _quests = new LiveQuestRegistry();
            _inventory = new FakeInventory();
            _rack = new BlankRack(2);
            // The runner registers each offered quest into the live registry the granter reads.
            _runner = new DialogueRunner(new DialogueSession(_fake), store, applier, parser,
                recorder: null, questRegistry: _quests, logger: logger);
        }

        private QuestRewardGranter Granter(QuestRewardPools pools) =>
            new QuestRewardGranter(_quests, _inventory, _rack,
                new QuestRewardRoller(pools, new FakeSeedProvider()), new FakeLogger());

        private static QuestRewardPools ArtifactOnlyPools() => new QuestRewardPools(
            new[]
            {
                new RewardArtifactOption("art_iron", 1, "power"),
                new RewardArtifactOption("art_rope", 1, "utility")
            },
            Array.Empty<RewardBlankOption>());

        private static QuestRewardPools BlankOnlyPools() => new QuestRewardPools(
            Array.Empty<RewardArtifactOption>(),
            new[]
            {
                new RewardBlankOption("blank.fox_leg", "fox"),
                new RewardBlankOption("blank.bare", "")
            });

        private static QuestData QuestWithRewards(params QuestRewardCore[] rewards) =>
            new QuestData("qst_clear_pass", "", "", Array.Empty<QuestObjective>(), new[] { "errand" },
                Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>(), rewards);

        private static Casting Cast(QuestData quest)
        {
            var actor = new NpcInstance("npc_07", "arch_road_bandit", "Razor", "free_blades");
            var dialogue = new DialogueData("dlg", "{}", "start", Array.Empty<string>(),
                Array.Empty<FactKeyShapeCore>(), new[] { "shakedown" });
            return new Casting(actor, dialogue, quest, null, new ContextBag());
        }

        private void DriveToCompletion(QuestData quest)
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"),
                FakeStoryManager.Frame.Line("done.", "complete-quest:"));
            _runner.Begin(Cast(quest));
            _runner.Continue(); // drive to the complete-quest line
            Assert.AreEqual(QuestState.Completed, _runner.ActiveQuest.State);
        }

        [Test]
        public void CompletedQuest_RollsArtifactOfDeclaredFamily_IntoInventory()
        {
            DriveToCompletion(QuestWithRewards(
                new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact)));

            Granter(ArtifactOnlyPools()).GrantFor(null);

            Assert.AreEqual(new[] { "art_iron" }, _inventory.Added,
                "the declared power belonging must be honoured by the roll");
        }

        [Test]
        public void CompletedQuest_RollsBlankOfDeclaredRace_OntoRack()
        {
            DriveToCompletion(QuestWithRewards(
                new QuestRewardCore(1, "fox", QuestRewardPayloadKind.PartBlank)));

            Granter(BlankOnlyPools()).GrantFor(null);

            Assert.IsEmpty(_inventory.Added);
            Assert.AreEqual(1, _rack.Blanks.Count);
            Assert.AreEqual("blank.fox_leg", _rack.Blanks[0].DefinitionId);
        }

        [Test]
        public void GrantingIsIdempotent_SecondCallGrantsNothing()
        {
            DriveToCompletion(QuestWithRewards(
                new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact)));

            var granter = Granter(ArtifactOnlyPools());
            granter.GrantFor(null);
            granter.GrantFor(null); // a later platform completion must not re-grant the already-paid quest

            Assert.AreEqual(1, _inventory.Added.Count);
        }

        [Test]
        public void EmptyRollPool_GrantsNothing_ButStillMarksPaid()
        {
            DriveToCompletion(QuestWithRewards(
                new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact)));

            var granter = Granter(new QuestRewardPools(null, null));
            granter.GrantFor(null);

            Assert.IsEmpty(_inventory.Added);
            Assert.IsTrue(_runner.ActiveQuest.RewardsGranted,
                "an unpayable declaration must not retry forever");
        }

        [Test]
        public void FullRack_ForfeitsTheBlank_WithoutThrowing()
        {
            DriveToCompletion(QuestWithRewards(
                new QuestRewardCore(1, "", QuestRewardPayloadKind.PartBlank)));
            Assert.IsTrue(_rack.TryAdd("blank.a", out _));
            Assert.IsTrue(_rack.TryAdd("blank.b", out _)); // capacity 2 reached

            Granter(BlankOnlyPools()).GrantFor(null);

            Assert.AreEqual(2, _rack.Blanks.Count, "the rack cap is never bypassed");
        }

        [Test]
        public void ActiveButNotCompletedQuest_GrantsNothing()
        {
            _fake.Script(FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"));
            _runner.Begin(Cast(QuestWithRewards(
                new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact))));
            Assert.AreEqual(QuestState.Active, _runner.ActiveQuest.State);

            Granter(ArtifactOnlyPools()).GrantFor(null);

            Assert.IsEmpty(_inventory.Added);
        }

        [Test]
        public void NoActiveQuest_GrantsNothing()
        {
            _fake.Script(FakeStoryManager.Frame.Line("just talk."));
            _runner.Begin(Cast(null));
            Assert.IsNull(_runner.ActiveQuest);

            Granter(ArtifactOnlyPools()).GrantFor(null);

            Assert.IsEmpty(_inventory.Added);
        }
    }
}
