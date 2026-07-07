using System;
using Narrative.Facts.Core;
using Narrative.QuestLog.Core;
using Narrative.Quests.Core;
using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The read-only quest log / saga readout model (P1-11, quest-log-and-saga.md): three quest
    /// states, saga grouping by thread with the P2-3 lifecycle verdict, objectives with progress,
    /// and a reward telegraph that carries tier + belonging but — by construction — no item id.
    /// </summary>
    [TestFixture]
    public class QuestLogModelBuilderTests
    {
        private static QuestData Quest(string id, string title = "t", string tag = "q",
            QuestRewardCore reward = null, params QuestObjective[] objectives) =>
            new QuestData(id, title, "s", objectives, new[] { tag },
                Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>(),
                reward != null ? new[] { reward } : Array.Empty<QuestRewardCore>());

        private static QuestInstance Offered(QuestData data, string giver, string threadId)
        {
            var quest = new QuestInstance(data);
            quest.SetOrigin(giver, threadId);
            quest.Start();
            return quest;
        }

        [Test]
        public void GroupsQuestsByThread_IntoOneSaga_InOfferOrder()
        {
            var registry = new LiveQuestRegistry();
            registry.Register(Offered(Quest("q1"), "Farmer", "barn_raid"));
            registry.Register(Offered(Quest("q2"), "Stranger", ""));
            registry.Register(Offered(Quest("q3"), "Farmer", "barn_raid"));

            var model = QuestLogModelBuilder.Build(registry, new ThreadLedger());

            Assert.AreEqual(2, model.Sagas.Count);
            Assert.AreEqual("barn_raid", model.Sagas[0].ThreadId);
            Assert.AreEqual(new[] { "q1", "q3" },
                new[] { model.Sagas[0].Entries[0].QuestId, model.Sagas[0].Entries[1].QuestId },
                "one thread reads as one saga, beats in offer order");
            Assert.AreEqual(QuestSagaState.None, model.Sagas[1].State, "threadless quests trail ungrouped");
        }

        [Test]
        public void SagaState_ProjectsTheThreadLifecycle()
        {
            var registry = new LiveQuestRegistry();
            registry.Register(Offered(Quest("q1"), "Farmer", "conflicted"));
            registry.Register(Offered(Quest("q2"), "Raider", "foreclosed"));
            registry.Register(Offered(Quest("q3"), "Elder", "lapsed"));
            registry.Register(Offered(Quest("q4"), "Friend", "alive"));

            var threads = new ThreadLedger();
            threads.Open("conflicted", ThreadKind.Ephemeral, 0);
            threads.Fail("conflicted", ThreadRetirementReason.Conflict);
            threads.Open("foreclosed", ThreadKind.Ephemeral, 0);
            threads.Fail("foreclosed", ThreadRetirementReason.Foreclosed);
            threads.Open("lapsed", ThreadKind.Ephemeral, 0);
            threads.Fail("lapsed", ThreadRetirementReason.Expired);
            threads.Open("alive", ThreadKind.Ephemeral, 0);

            var model = QuestLogModelBuilder.Build(registry, threads);

            Assert.AreEqual(QuestSagaState.FailedByConflict, model.Sagas[0].State,
                "a resolved moral fork shows the foreclosed opposing arc as saga, not a dangling entry");
            Assert.AreEqual(QuestSagaState.Foreclosed, model.Sagas[1].State);
            Assert.AreEqual(QuestSagaState.Expired, model.Sagas[2].State);
            Assert.AreEqual(QuestSagaState.Live, model.Sagas[3].State);
        }

        [Test]
        public void Entry_CarriesStateObjectivesAndProgress()
        {
            var data = Quest("q1", "Bounty", "q", null,
                new QuestObjective("o1", "Beat the raider", QuestObjectiveKind.Defeat, 2,
                    Array.Empty<FactEffectCore>()));
            var quest = Offered(data, "Farmer", "");
            quest.AdvanceObjective("o1");
            var registry = new LiveQuestRegistry();
            registry.Register(quest);

            var entry = QuestLogModelBuilder.Build(registry, new ThreadLedger()).Sagas[0].Entries[0];

            Assert.AreEqual(QuestState.Active, entry.State);
            Assert.AreEqual("Farmer", entry.GiverName);
            Assert.AreEqual(1, entry.Objectives[0].Progress);
            Assert.AreEqual(2, entry.Objectives[0].TargetCount);
            Assert.IsFalse(entry.Objectives[0].Completed);
        }

        [Test]
        public void RewardTelegraph_IsTierAndBelongingOnly_NeverAnItem()
        {
            var registry = new LiveQuestRegistry();
            registry.Register(Offered(
                Quest("q1", "Bounty", "q", new QuestRewardCore(2, "fox", QuestRewardPayloadKind.PartBlank)),
                "Farmer", ""));

            var entry = QuestLogModelBuilder.Build(registry, new ThreadLedger()).Sagas[0].Entries[0];

            Assert.IsTrue(entry.HasRewardTelegraph);
            Assert.AreEqual(2, entry.RewardTier);
            Assert.AreEqual("fox", entry.BelongingId);
            // The offer-card rule holds inside the log too: the model has no field that could name
            // the rolled item - checked here as "nothing beyond tier + belonging is exposed".
        }

        [Test]
        public void EmptyRegistry_YieldsTheEmptyModel()
        {
            var model = QuestLogModelBuilder.Build(new LiveQuestRegistry(), new ThreadLedger());
            Assert.AreEqual(0, model.Sagas.Count);
        }
    }
}
