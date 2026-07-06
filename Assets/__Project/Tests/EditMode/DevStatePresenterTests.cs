using System;
using System.Collections.Generic;
using System.Linq;
using CharacterProgression.Core;
using DevTools;
using DevTools.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DevStatePresenterTests
    {
        private sealed class FakeProgression : IRunProgressionRecord
        {
            public List<string> Active = new();
            public List<string> Completed = new();
            public List<string> Failed = new();

            public QuestStatus? GetQuestStatus(string questId) =>
                Active.Contains(questId) ? QuestStatus.Active
                : Completed.Contains(questId) ? QuestStatus.Completed
                : Failed.Contains(questId) ? QuestStatus.Failed
                : (QuestStatus?)null;

            public bool IsQuestActive(string questId) => Active.Contains(questId);
            public bool IsQuestCompleted(string questId) => Completed.Contains(questId);
            public bool IsQuestFailed(string questId) => Failed.Contains(questId);
            public bool HasEncounteredNpc(string npcId) => false;
            public bool TryGetChoice(string key, out string value) { value = null; return false; }
            public IReadOnlyCollection<string> ActiveQuests => Active;
            public IReadOnlyCollection<string> CompletedQuests => Completed;
            public IReadOnlyCollection<string> FailedQuests => Failed;
            public IReadOnlyCollection<string> EncounteredNpcs => Array.Empty<string>();
            public IReadOnlyDictionary<string, string> Choices => new Dictionary<string, string>();
        }

        private sealed class FakeLibrary : IFragmentLibrary
        {
            public List<QuestData> Quests = new();
            public IReadOnlyList<DialogueData> FindDialogues(IReadOnlyList<string> requiredTags) => Array.Empty<DialogueData>();
            public IReadOnlyList<QuestData> FindQuests(IReadOnlyList<string> requiredTags) => Quests;
            public IReadOnlyList<EnemyFragment> FindEnemies(IReadOnlyList<string> requiredTags) => Array.Empty<EnemyFragment>();
        }

        private sealed class FakeFactStore : IFactStore
        {
            public List<KeyValuePair<FactKey, FactValue>> Facts = new();
            public bool TryGet(FactKey key, out FactValue value) { value = default; return false; }
            public FactValue GetOrDefault(FactKey key, FactValue fallback) => fallback;
            public bool Has(FactKey key) => false;
            public void Set(FactKey key, FactValue value) { }
            public bool Remove(FactKey key) => false;
            public IReadOnlyList<KeyValuePair<FactKey, FactValue>> Snapshot() => Facts;
            public event Action<FactKey, FactValue> OnFactChanged { add { } remove { } }
        }

        private static QuestData Quest(string id, string name) =>
            new QuestData(id, name, "", Array.Empty<QuestObjective>(), Array.Empty<string>(),
                Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>());

        private static DevPanelSection Section(IReadOnlyList<DevPanelSection> sections, string title) =>
            sections.First(s => s.Title == title);

        [Test]
        public void QuestsSection_GroupsByStatus_AndMapsNames()
        {
            var progression = new FakeProgression { Active = { "qst_a" }, Completed = { "qst_b" }, Failed = { "qst_c" } };
            var library = new FakeLibrary { Quests = { Quest("qst_a", "Alpha"), Quest("qst_b", "Beta"), Quest("qst_c", "Gamma") } };
            var presenter = new DevStatePresenter(progression, library, new FakeFactStore());

            var quests = Section(presenter.BuildSections(), "Quests").Rows;

            Assert.Contains("Alpha (qst_a): Active", quests.ToList());
            Assert.Contains("Beta (qst_b): Completed", quests.ToList());
            Assert.Contains("Gamma (qst_c): Failed", quests.ToList());
            CollectionAssert.Contains(quests, "Active 1 | Completed 1 | Failed 1");
        }

        [Test]
        public void QuestsSection_FallsBackToId_WhenLibraryHasNoName()
        {
            var progression = new FakeProgression { Active = { "qst_unmapped" } };
            var presenter = new DevStatePresenter(progression, new FakeLibrary(), new FakeFactStore());

            var quests = Section(presenter.BuildSections(), "Quests").Rows;

            Assert.Contains("qst_unmapped (qst_unmapped): Active", quests.ToList());
        }

        [Test]
        public void QuestsSection_ReportsEmpty_WhenNothingRecorded()
        {
            var presenter = new DevStatePresenter(new FakeProgression(), new FakeLibrary(), new FakeFactStore());
            var quests = Section(presenter.BuildSections(), "Quests").Rows;
            Assert.Contains("(no quests recorded)", quests.ToList());
        }

        [Test]
        public void FactsSection_MirrorsSnapshot_WithTypedValue()
        {
            var store = new FakeFactStore
            {
                Facts =
                {
                    new KeyValuePair<FactKey, FactValue>(FactKey.Global(FactNamespace.World, "barn_raided"), FactValue.FromBool(true)),
                    new KeyValuePair<FactKey, FactValue>(new FactKey(FactNamespace.Faction, "free_blades", "reputation"), FactValue.FromInt(5))
                }
            };
            var presenter = new DevStatePresenter(new FakeProgression(), new FakeLibrary(), store);

            var facts = Section(presenter.BuildSections(), "Director Facts").Rows;

            Assert.Contains("world.barn_raided = true [Bool]", facts.ToList());
            Assert.Contains("faction.free_blades.reputation = 5 [Int]", facts.ToList());
            CollectionAssert.Contains(facts, "2 fact(s)");
        }

        [Test]
        public void FactsSection_ReportsEmpty_WhenStoreEmpty()
        {
            var presenter = new DevStatePresenter(new FakeProgression(), new FakeLibrary(), new FakeFactStore());
            var facts = Section(presenter.BuildSections(), "Director Facts").Rows;
            Assert.Contains("(none set)", facts.ToList());
        }
    }
}
