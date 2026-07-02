using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Loot.Application;
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
        }

        private FakeStoryManager _fake;
        private DialogueRunner _runner;
        private LiveQuestRegistry _quests;

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
            // The runner registers each offered quest into the live registry the granter reads.
            _runner = new DialogueRunner(new DialogueSession(_fake), store, applier, parser,
                recorder: null, questRegistry: _quests, logger: logger);
        }

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

        [Test]
        public void CompletedQuest_GrantsEachRewardByCount()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"),
                FakeStoryManager.Frame.Line("done.", "complete-quest:"));
            _runner.Begin(Cast(QuestWithRewards(new QuestRewardCore("art_apple", 2), new QuestRewardCore("art_coin", 1))));
            _runner.Continue(); // drive to the complete-quest line
            Assert.AreEqual(QuestState.Completed, _runner.ActiveQuest.State);

            var inventory = new FakeInventory();
            var granter = new QuestRewardGranter(_quests, inventory, new FakeLogger());
            granter.GrantFor(null);

            Assert.AreEqual(new[] { "art_apple", "art_apple", "art_coin" }, inventory.Added);
        }

        [Test]
        public void GrantingIsIdempotent_SecondCallGrantsNothing()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"),
                FakeStoryManager.Frame.Line("done.", "complete-quest:"));
            _runner.Begin(Cast(QuestWithRewards(new QuestRewardCore("art_apple", 1))));
            _runner.Continue();

            var inventory = new FakeInventory();
            var granter = new QuestRewardGranter(_quests, inventory, new FakeLogger());
            granter.GrantFor(null);
            granter.GrantFor(null); // a later platform completion must not re-grant the already-paid quest

            Assert.AreEqual(new[] { "art_apple" }, inventory.Added);
        }

        [Test]
        public void ActiveButNotCompletedQuest_GrantsNothing()
        {
            _fake.Script(FakeStoryManager.Frame.Line("deal?", "offer-quest: errand"));
            _runner.Begin(Cast(QuestWithRewards(new QuestRewardCore("art_apple", 1))));
            Assert.AreEqual(QuestState.Active, _runner.ActiveQuest.State);

            var inventory = new FakeInventory();
            new QuestRewardGranter(_quests, inventory, new FakeLogger()).GrantFor(null);

            Assert.IsEmpty(inventory.Added);
        }

        [Test]
        public void NoActiveQuest_GrantsNothing()
        {
            _fake.Script(FakeStoryManager.Frame.Line("just talk."));
            _runner.Begin(Cast(null));
            Assert.IsNull(_runner.ActiveQuest);

            var inventory = new FakeInventory();
            new QuestRewardGranter(_quests, inventory, new FakeLogger()).GrantFor(null);

            Assert.IsEmpty(inventory.Added);
        }
    }
}
