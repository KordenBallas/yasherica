using System.Collections.Generic;

namespace CharacterProgression.Core
{
    /// <summary>
    /// In-memory per-run progression state. Single implementation of both the
    /// read (<see cref="IRunProgressionRecord"/>) and write
    /// (<see cref="IRunProgressionRecorder"/>) surfaces.
    ///
    /// Status precedence: a terminal status (Completed / Failed) wins over Active.
    /// <see cref="StartQuest"/> will not demote a quest that already reached a
    /// terminal status; <see cref="CompleteQuest"/> / <see cref="FailQuest"/>
    /// always override. Pure C#, no UnityEngine.
    /// </summary>
    public class RunProgressionRecord : IRunProgressionRecord, IRunProgressionRecorder
    {
        private readonly Dictionary<string, QuestStatus> _quests = new();
        private readonly HashSet<string> _encounteredNpcs = new();
        private readonly Dictionary<string, string> _choices = new();

        public QuestStatus? GetQuestStatus(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return null;

            return _quests.TryGetValue(questId, out var status) ? status : (QuestStatus?)null;
        }

        public bool IsQuestActive(string questId) => GetQuestStatus(questId) == QuestStatus.Active;
        public bool IsQuestCompleted(string questId) => GetQuestStatus(questId) == QuestStatus.Completed;
        public bool IsQuestFailed(string questId) => GetQuestStatus(questId) == QuestStatus.Failed;

        public bool HasEncounteredNpc(string npcId) =>
            !string.IsNullOrEmpty(npcId) && _encounteredNpcs.Contains(npcId);

        public bool TryGetChoice(string key, out string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = null;
                return false;
            }

            return _choices.TryGetValue(key, out value);
        }

        public IReadOnlyCollection<string> ActiveQuests => QuestsWithStatus(QuestStatus.Active);
        public IReadOnlyCollection<string> CompletedQuests => QuestsWithStatus(QuestStatus.Completed);
        public IReadOnlyCollection<string> FailedQuests => QuestsWithStatus(QuestStatus.Failed);
        public IReadOnlyCollection<string> EncounteredNpcs => _encounteredNpcs;
        public IReadOnlyDictionary<string, string> Choices => _choices;

        public void StartQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return;

            // Do not demote a quest that already reached a terminal status.
            if (_quests.TryGetValue(questId, out var existing) && existing != QuestStatus.Active)
                return;

            _quests[questId] = QuestStatus.Active;
        }

        public void CompleteQuest(string questId) => SetTerminal(questId, QuestStatus.Completed);

        public void FailQuest(string questId) => SetTerminal(questId, QuestStatus.Failed);

        public void RecordNpcEncounter(string npcId)
        {
            if (!string.IsNullOrEmpty(npcId))
                _encounteredNpcs.Add(npcId);
        }

        public void RecordChoice(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _choices[key] = value;
        }

        private void SetTerminal(string questId, QuestStatus status)
        {
            if (string.IsNullOrEmpty(questId))
                return;

            _quests[questId] = status;
        }

        private List<string> QuestsWithStatus(QuestStatus status)
        {
            var result = new List<string>();
            foreach (var pair in _quests)
            {
                if (pair.Value == status)
                    result.Add(pair.Key);
            }

            return result;
        }
    }
}
