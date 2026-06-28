using System.Collections.Generic;

namespace Narrative.Quests.Core
{
    /// <summary>
    /// Default <see cref="ILiveQuestRegistry"/>: a pure-C# list of offered quests kept in registration
    /// order. Offer order is driven by the seeded planner, so iterating <see cref="LiveQuests"/> is
    /// replay-stable. Registration is idempotent on <see cref="QuestData.QuestId"/> so a quest re-seen by a
    /// later dialogue is not duplicated.
    /// </summary>
    public sealed class LiveQuestRegistry : ILiveQuestRegistry
    {
        private readonly List<QuestInstance> _quests = new List<QuestInstance>();

        public IReadOnlyList<QuestInstance> LiveQuests => _quests;

        public bool TryGet(string questId, out QuestInstance quest)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (string.Equals(_quests[i].Data.QuestId, questId, System.StringComparison.Ordinal))
                {
                    quest = _quests[i];
                    return true;
                }
            }

            quest = null;
            return false;
        }

        public void Register(QuestInstance quest)
        {
            if (quest == null)
            {
                return;
            }

            if (TryGet(quest.Data.QuestId, out _))
            {
                return;
            }

            _quests.Add(quest);
        }
    }
}
