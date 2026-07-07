using System.Collections.Generic;
using Narrative.Quests.Core;
using Narrative.Threads.Core;

namespace Narrative.QuestLog.Core
{
    /// <summary>
    /// Builds the read-only quest-log/saga model (P1-11) from state that already exists — the live
    /// quest registry and the thread ledger. Pure projection: it mutates nothing and adds no data a
    /// quest/thread doesn't already declare. Quests sharing a thread group into one saga in offer
    /// order; threadless quests trail as their own single-entry groups.
    /// </summary>
    public static class QuestLogModelBuilder
    {
        public static QuestLogModel Build(ILiveQuestRegistry quests, IThreadLedger threads)
        {
            var live = quests?.LiveQuests;
            if (live == null || live.Count == 0)
            {
                return QuestLogModel.Empty;
            }

            var sagaOrder = new List<string>();
            var entriesByThread = new Dictionary<string, List<QuestLogEntry>>();
            var threadless = new List<QuestLogEntry>();

            for (int i = 0; i < live.Count; i++)
            {
                var entry = ToEntry(live[i]);
                var threadId = live[i].ThreadId;
                if (string.IsNullOrEmpty(threadId))
                {
                    threadless.Add(entry);
                    continue;
                }

                if (!entriesByThread.TryGetValue(threadId, out var list))
                {
                    list = new List<QuestLogEntry>();
                    entriesByThread.Add(threadId, list);
                    sagaOrder.Add(threadId);
                }

                list.Add(entry);
            }

            var sagas = new List<QuestSaga>(sagaOrder.Count + threadless.Count);
            for (int i = 0; i < sagaOrder.Count; i++)
            {
                var threadId = sagaOrder[i];
                sagas.Add(new QuestSaga(threadId, SagaStateOf(threadId, threads), entriesByThread[threadId]));
            }

            for (int i = 0; i < threadless.Count; i++)
            {
                sagas.Add(new QuestSaga(string.Empty, QuestSagaState.None, new[] { threadless[i] }));
            }

            return new QuestLogModel(sagas);
        }

        private static QuestLogEntry ToEntry(QuestInstance quest)
        {
            var data = quest.Data;
            var objectives = new List<QuestLogObjective>(data.Objectives.Count);
            for (int i = 0; i < data.Objectives.Count; i++)
            {
                var objective = data.Objectives[i];
                objectives.Add(new QuestLogObjective(
                    objective.Description,
                    quest.ProgressOf(objective.ObjectiveId),
                    objective.TargetCount,
                    quest.IsObjectiveComplete(objective.ObjectiveId)));
            }

            bool hasReward = data.Rewards.Count > 0;
            return new QuestLogEntry(
                data.QuestId,
                data.DisplayName,
                quest.GiverDisplayName,
                data.Summary,
                quest.State,
                objectives,
                hasReward,
                hasReward ? data.Rewards[0].Tier : 0,
                hasReward ? data.Rewards[0].BelongingId : string.Empty);
        }

        private static QuestSagaState SagaStateOf(string threadId, IThreadLedger threads)
        {
            if (threads == null || !threads.TryGet(threadId, out var record))
            {
                // A quest may carry a thread label the ledger never opened (e.g. restored edge
                // cases); it still reads as one saga, just without a lifecycle verdict.
                return QuestSagaState.None;
            }

            switch (record.State)
            {
                case ThreadState.Resolved:
                    return QuestSagaState.Completed;
                case ThreadState.Failed:
                    switch (record.Reason)
                    {
                        case ThreadRetirementReason.Foreclosed:
                            return QuestSagaState.Foreclosed;
                        case ThreadRetirementReason.Expired:
                            return QuestSagaState.Expired;
                        default:
                            return QuestSagaState.FailedByConflict;
                    }

                default:
                    return QuestSagaState.Live;
            }
        }
    }
}
