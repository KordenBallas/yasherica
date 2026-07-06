using System.Collections.Generic;

namespace CharacterProgression.Core
{
    /// <summary>
    /// Read-only view of the current run's progression state: quests by status,
    /// NPCs encountered, and key choices. Consumed by systems that react to run
    /// state (e.g. narrative reward gating). Pure C#, no UnityEngine.
    /// </summary>
    public interface IRunProgressionRecord
    {
        /// <summary>
        /// Status of the quest, or null if the quest has never been recorded this run.
        /// </summary>
        QuestStatus? GetQuestStatus(string questId);

        bool IsQuestActive(string questId);
        bool IsQuestCompleted(string questId);
        bool IsQuestFailed(string questId);

        /// <summary>True if the NPC has been encountered (dialogue started) this run.</summary>
        bool HasEncounteredNpc(string npcId);

        /// <summary>Returns the recorded value for a choice key, or true if it exists.</summary>
        bool TryGetChoice(string key, out string value);

        IReadOnlyCollection<string> ActiveQuests { get; }
        IReadOnlyCollection<string> CompletedQuests { get; }
        IReadOnlyCollection<string> FailedQuests { get; }
        IReadOnlyCollection<string> EncounteredNpcs { get; }

        /// <summary>All recorded key choices — enumerable so the save layer can capture them (P2-2).</summary>
        IReadOnlyDictionary<string, string> Choices { get; }
    }
}
