using System.Collections.Generic;
using CharacterProgression.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class QuestInstanceTests
    {
        private sealed class FakeRecorder : IRunProgressionRecorder
        {
            public readonly List<string> Started = new();
            public readonly List<string> Completed = new();
            public readonly List<string> Failed = new();
            public void StartQuest(string questId) => Started.Add(questId);
            public void CompleteQuest(string questId) => Completed.Add(questId);
            public void FailQuest(string questId) => Failed.Add(questId);
            public void RecordNpcEncounter(string npcId) { }
            public void RecordChoice(string key, string value) { }
        }

        private static FactEffectCore SetBool(string key) =>
            new FactEffectCore(FactNamespace.World, "", key, FactEffectOp.Set, FactValue.FromBool(true));

        private static QuestData Quest()
        {
            var objective = new QuestObjective("pay_or_defeat", "", QuestObjectiveKind.Choice, 1, System.Array.Empty<FactEffectCore>());
            return new QuestData("qst_clear_pass", "Clear the Pass", "", new[] { objective },
                new[] { "errand" }, new[] { SetBool("pass_cleared") }, new[] { SetBool("pass_failed") });
        }

        [Test]
        public void Lifecycle_StartCompleteEmitsEffects_AndBridgesRecorder()
        {
            var recorder = new FakeRecorder();
            var quest = new QuestInstance(Quest(), recorder);

            Assert.AreEqual(QuestState.NotStarted, quest.State);
            quest.Start();
            Assert.AreEqual(QuestState.Active, quest.State);
            CollectionAssert.Contains(recorder.Started, "qst_clear_pass");

            var objEffects = quest.AdvanceObjective("pay_or_defeat");
            Assert.IsTrue(quest.IsObjectiveComplete("pay_or_defeat"));
            Assert.AreEqual(0, objEffects.Count); // no objective effects in this quest
            Assert.IsTrue(quest.AllObjectivesComplete);

            var completeEffects = quest.Complete();
            Assert.AreEqual(QuestState.Completed, quest.State);
            Assert.AreEqual(1, completeEffects.Count);
            CollectionAssert.Contains(recorder.Completed, "qst_clear_pass");
        }

        [Test]
        public void Fail_EmitsFailEffects_AndBridgesRecorder()
        {
            var recorder = new FakeRecorder();
            var quest = new QuestInstance(Quest(), recorder);
            quest.Start();

            var failEffects = quest.Fail();
            Assert.AreEqual(QuestState.Failed, quest.State);
            Assert.AreEqual(1, failEffects.Count);
            CollectionAssert.Contains(recorder.Failed, "qst_clear_pass");
        }

        [Test]
        public void AdvanceObjective_BeforeStart_IsNoOp()
        {
            var quest = new QuestInstance(Quest());
            var effects = quest.AdvanceObjective("pay_or_defeat");
            Assert.AreEqual(0, effects.Count);
            Assert.IsFalse(quest.IsObjectiveComplete("pay_or_defeat"));
        }

        [Test]
        public void Footprint_CoversCompleteAndFailEffects()
        {
            var quest = new QuestInstance(Quest());
            Assert.AreEqual(2, quest.Footprint.Count); // pass_cleared + pass_failed
        }
    }
}
