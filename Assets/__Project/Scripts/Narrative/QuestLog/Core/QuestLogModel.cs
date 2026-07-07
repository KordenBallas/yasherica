using System;
using System.Collections.Generic;

namespace Narrative.QuestLog.Core
{
    /// <summary>One objective row of a quest-log entry (description + progress).</summary>
    public sealed class QuestLogObjective
    {
        public string Description { get; }
        public int Progress { get; }
        public int TargetCount { get; }
        public bool Completed { get; }

        public QuestLogObjective(string description, int progress, int targetCount, bool completed)
        {
            Description = description ?? string.Empty;
            Progress = progress;
            TargetCount = targetCount;
            Completed = completed;
        }
    }

    /// <summary>
    /// One quest in the log (P1-11): the job (title / giver / summary / objectives) plus the reward
    /// telegraph exactly as the offer card promised — tier + belonging, NEVER the rolled item. The
    /// model carries no item id by construction, so the log cannot become a reward catalog.
    /// </summary>
    public sealed class QuestLogEntry
    {
        public string QuestId { get; }
        public string Title { get; }
        public string GiverName { get; }
        public string Summary { get; }
        public Quests.Core.QuestState State { get; }
        public IReadOnlyList<QuestLogObjective> Objectives { get; }
        public bool HasRewardTelegraph { get; }
        public int RewardTier { get; }
        public string BelongingId { get; }

        public QuestLogEntry(string questId, string title, string giverName, string summary,
            Quests.Core.QuestState state, IReadOnlyList<QuestLogObjective> objectives,
            bool hasRewardTelegraph, int rewardTier, string belongingId)
        {
            QuestId = questId ?? string.Empty;
            Title = title ?? string.Empty;
            GiverName = giverName ?? string.Empty;
            Summary = summary ?? string.Empty;
            State = state;
            Objectives = objectives ?? Array.Empty<QuestLogObjective>();
            HasRewardTelegraph = hasRewardTelegraph;
            RewardTier = rewardTier;
            BelongingId = belongingId ?? string.Empty;
        }
    }

    /// <summary>The saga readout's thread state (a projection of the P2-3 thread lifecycle).</summary>
    public enum QuestSagaState
    {
        /// <summary>Threadless quests (no saga grouping).</summary>
        None = 0,
        Live = 1,
        Completed = 2,
        FailedByConflict = 3,
        Foreclosed = 4,
        Expired = 5
    }

    /// <summary>One saga: a thread with its member quests in offer order (P1-11 FR5/FR6).</summary>
    public sealed class QuestSaga
    {
        public string ThreadId { get; }
        public QuestSagaState State { get; }
        public IReadOnlyList<QuestLogEntry> Entries { get; }

        public QuestSaga(string threadId, QuestSagaState state, IReadOnlyList<QuestLogEntry> entries)
        {
            ThreadId = threadId ?? string.Empty;
            State = state;
            Entries = entries ?? Array.Empty<QuestLogEntry>();
        }
    }

    /// <summary>The whole read-only quest log: sagas in first-offer order, threadless quests last.</summary>
    public sealed class QuestLogModel
    {
        public static readonly QuestLogModel Empty = new QuestLogModel(Array.Empty<QuestSaga>());

        public IReadOnlyList<QuestSaga> Sagas { get; }

        public QuestLogModel(IReadOnlyList<QuestSaga> sagas)
        {
            Sagas = sagas ?? Array.Empty<QuestSaga>();
        }
    }
}
