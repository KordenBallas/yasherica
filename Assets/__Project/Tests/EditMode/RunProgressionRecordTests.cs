using CharacterProgression.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunProgressionRecordTests
    {
        private RunProgressionRecord _record;

        [SetUp]
        public void SetUp()
        {
            _record = new RunProgressionRecord();
        }

        [Test]
        public void NewRecord_IsEmpty()
        {
            Assert.IsNull(_record.GetQuestStatus("q"));
            Assert.IsFalse(_record.HasEncounteredNpc("npc"));
            CollectionAssert.IsEmpty(_record.ActiveQuests);
            CollectionAssert.IsEmpty(_record.CompletedQuests);
            CollectionAssert.IsEmpty(_record.FailedQuests);
            CollectionAssert.IsEmpty(_record.EncounteredNpcs);
        }

        [Test]
        public void StartQuest_MarksActive()
        {
            _record.StartQuest("q1");

            Assert.AreEqual(QuestStatus.Active, _record.GetQuestStatus("q1"));
            Assert.IsTrue(_record.IsQuestActive("q1"));
            CollectionAssert.AreEquivalent(new[] { "q1" }, _record.ActiveQuests);
        }

        [Test]
        public void CompleteQuest_OverridesActive()
        {
            _record.StartQuest("q1");
            _record.CompleteQuest("q1");

            Assert.IsTrue(_record.IsQuestCompleted("q1"));
            Assert.IsFalse(_record.IsQuestActive("q1"));
            CollectionAssert.IsEmpty(_record.ActiveQuests);
            CollectionAssert.AreEquivalent(new[] { "q1" }, _record.CompletedQuests);
        }

        [Test]
        public void FailQuest_OverridesActive()
        {
            _record.StartQuest("q1");
            _record.FailQuest("q1");

            Assert.IsTrue(_record.IsQuestFailed("q1"));
            CollectionAssert.AreEquivalent(new[] { "q1" }, _record.FailedQuests);
        }

        [Test]
        public void StartQuest_DoesNotDemoteCompletedQuest()
        {
            _record.CompleteQuest("q1");
            _record.StartQuest("q1");

            Assert.IsTrue(_record.IsQuestCompleted("q1"));
            Assert.IsFalse(_record.IsQuestActive("q1"));
        }

        [Test]
        public void StartQuest_DoesNotDemoteFailedQuest()
        {
            _record.FailQuest("q1");
            _record.StartQuest("q1");

            Assert.IsTrue(_record.IsQuestFailed("q1"));
        }

        [Test]
        public void CompleteThenFail_LastTerminalWins()
        {
            _record.CompleteQuest("q1");
            _record.FailQuest("q1");

            Assert.IsTrue(_record.IsQuestFailed("q1"));
        }

        [Test]
        public void StartQuest_IsIdempotent()
        {
            _record.StartQuest("q1");
            _record.StartQuest("q1");

            Assert.AreEqual(1, System.Linq.Enumerable.Count(_record.ActiveQuests));
        }

        [Test]
        public void QuestMethods_NullOrEmptyId_AreSafeNoOps()
        {
            _record.StartQuest(null);
            _record.StartQuest("");
            _record.CompleteQuest(null);
            _record.FailQuest("");

            CollectionAssert.IsEmpty(_record.ActiveQuests);
            CollectionAssert.IsEmpty(_record.CompletedQuests);
            CollectionAssert.IsEmpty(_record.FailedQuests);
            Assert.IsNull(_record.GetQuestStatus(null));
            Assert.IsNull(_record.GetQuestStatus(""));
        }

        [Test]
        public void RecordNpcEncounter_TracksNpc()
        {
            _record.RecordNpcEncounter("elder");

            Assert.IsTrue(_record.HasEncounteredNpc("elder"));
            Assert.IsFalse(_record.HasEncounteredNpc("stranger"));
            CollectionAssert.AreEquivalent(new[] { "elder" }, _record.EncounteredNpcs);
        }

        [Test]
        public void RecordNpcEncounter_IsIdempotent()
        {
            _record.RecordNpcEncounter("elder");
            _record.RecordNpcEncounter("elder");

            Assert.AreEqual(1, System.Linq.Enumerable.Count(_record.EncounteredNpcs));
        }

        [Test]
        public void RecordNpcEncounter_NullOrEmptyId_IsSafeNoOp()
        {
            _record.RecordNpcEncounter(null);
            _record.RecordNpcEncounter("");

            CollectionAssert.IsEmpty(_record.EncounteredNpcs);
            Assert.IsFalse(_record.HasEncounteredNpc(null));
            Assert.IsFalse(_record.HasEncounteredNpc(""));
        }

        [Test]
        public void RecordChoice_StoresAndOverwrites()
        {
            _record.RecordChoice("ending", "rebels");
            Assert.IsTrue(_record.TryGetChoice("ending", out var first));
            Assert.AreEqual("rebels", first);

            _record.RecordChoice("ending", "empire");
            Assert.IsTrue(_record.TryGetChoice("ending", out var second));
            Assert.AreEqual("empire", second);
        }

        [Test]
        public void TryGetChoice_UnknownOrNullKey_ReturnsFalse()
        {
            Assert.IsFalse(_record.TryGetChoice("missing", out _));
            Assert.IsFalse(_record.TryGetChoice(null, out _));
            Assert.IsFalse(_record.TryGetChoice("", out _));
        }

        [Test]
        public void RecordChoice_NullOrEmptyKey_IsSafeNoOp()
        {
            _record.RecordChoice(null, "v");
            _record.RecordChoice("", "v");

            Assert.IsFalse(_record.TryGetChoice(null, out _));
            Assert.IsFalse(_record.TryGetChoice("", out _));
        }
    }
}
