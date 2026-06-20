using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Quests.Core
{
    /// <summary>Kind of goal an objective tracks.</summary>
    public enum QuestObjectiveKind
    {
        Reach = 0,
        Defeat = 1,
        Deliver = 2,
        Choice = 3
    }

    /// <summary>
    /// Immutable definition of one quest objective: id, description, kind, target count, and the fact
    /// effects emitted when it completes (R7 footprint contribution). UnityEngine-free.
    /// </summary>
    public sealed class QuestObjective
    {
        public string ObjectiveId { get; }
        public string Description { get; }
        public QuestObjectiveKind Kind { get; }
        public int TargetCount { get; }
        public IReadOnlyList<FactEffectCore> CompletionEffects { get; }

        public QuestObjective(string objectiveId, string description, QuestObjectiveKind kind, int targetCount,
            IReadOnlyList<FactEffectCore> completionEffects)
        {
            ObjectiveId = objectiveId ?? string.Empty;
            Description = description ?? string.Empty;
            Kind = kind;
            TargetCount = targetCount < 1 ? 1 : targetCount;
            CompletionEffects = completionEffects ?? System.Array.Empty<FactEffectCore>();
        }
    }
}
