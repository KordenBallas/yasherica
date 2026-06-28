using System;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class LiveQuestRegistryTests
    {
        private static QuestInstance Quest(string id) =>
            new QuestInstance(new QuestData(id, "", "", Array.Empty<QuestObjective>(), Array.Empty<string>(),
                Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>()));

        [Test]
        public void Register_ThenTryGet_ReturnsSameInstance()
        {
            var registry = new LiveQuestRegistry();
            var quest = Quest("qst_a");

            registry.Register(quest);

            Assert.IsTrue(registry.TryGet("qst_a", out var found));
            Assert.AreSame(quest, found);
        }

        [Test]
        public void Register_IsIdempotentOnQuestId_KeepsFirstInstance()
        {
            var registry = new LiveQuestRegistry();
            var first = Quest("qst_a");
            var second = Quest("qst_a"); // same id, different instance

            registry.Register(first);
            registry.Register(second);

            Assert.AreEqual(1, registry.LiveQuests.Count);
            Assert.IsTrue(registry.TryGet("qst_a", out var found));
            Assert.AreSame(first, found); // the original live instance is preserved
        }

        [Test]
        public void Register_IgnoresNull()
        {
            var registry = new LiveQuestRegistry();

            registry.Register(null);

            Assert.AreEqual(0, registry.LiveQuests.Count);
        }

        [Test]
        public void TryGet_Miss_ReturnsFalseAndNull()
        {
            var registry = new LiveQuestRegistry();
            registry.Register(Quest("qst_a"));

            Assert.IsFalse(registry.TryGet("qst_missing", out var found));
            Assert.IsNull(found);
        }

        [Test]
        public void LiveQuests_PreservesRegistrationOrder()
        {
            var registry = new LiveQuestRegistry();
            registry.Register(Quest("qst_a"));
            registry.Register(Quest("qst_b"));
            registry.Register(Quest("qst_c"));

            Assert.AreEqual("qst_a", registry.LiveQuests[0].Data.QuestId);
            Assert.AreEqual("qst_b", registry.LiveQuests[1].Data.QuestId);
            Assert.AreEqual("qst_c", registry.LiveQuests[2].Data.QuestId);
        }
    }
}
