using System;
using System.Collections.Generic;
using CharacterProgression.Core;
using Core.Logging;
using Narrative.Casting.Core;
using Narrative.Quests.Core;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>
    /// Pure bridge between <see cref="ILiveQuestRegistry"/> and its serializable snapshots. Capture
    /// reads the public quest surface (state, per-objective counts, reward guard). Restore replays
    /// the lifecycle (Start → advance counts → Complete/Fail) on a fresh instance built over the
    /// <see cref="QuestData"/> resolved by id from the fragment library — the transition effects are
    /// deliberately DISCARDED (the resulting facts are already in the restored fact store), while the
    /// recorder bridge inside <see cref="QuestInstance"/> repopulates the run progression record for
    /// free. A quest whose data no longer exists is skipped with a warning (FR14 tolerance).
    /// </summary>
    public static class QuestSnapshotMapper
    {
        public static List<QuestInstanceSnapshot> Capture(ILiveQuestRegistry registry)
        {
            var snapshots = new List<QuestInstanceSnapshot>();
            if (registry == null)
            {
                return snapshots;
            }

            foreach (var quest in registry.LiveQuests)
            {
                var snapshot = new QuestInstanceSnapshot
                {
                    QuestId = quest.Data.QuestId,
                    State = (int)quest.State,
                    RewardsGranted = quest.RewardsGranted
                };

                foreach (var objective in quest.Data.Objectives)
                {
                    snapshot.ObjectiveIds.Add(objective.ObjectiveId);
                    snapshot.ObjectiveCounts.Add(quest.ProgressOf(objective.ObjectiveId));
                    if (quest.IsObjectiveComplete(objective.ObjectiveId))
                    {
                        snapshot.CompletedObjectives.Add(objective.ObjectiveId);
                    }
                }

                snapshots.Add(snapshot);
            }

            return snapshots;
        }

        public static void Restore(IReadOnlyList<QuestInstanceSnapshot> snapshots, ILiveQuestRegistry registry,
            IFragmentLibrary library, IRunProgressionRecorder recorder, IGameLogger logger = null)
        {
            if (snapshots == null || snapshots.Count == 0 || registry == null || library == null)
            {
                return;
            }

            var dataById = IndexQuestData(library);
            foreach (var snapshot in snapshots)
            {
                if (!dataById.TryGetValue(snapshot.QuestId ?? string.Empty, out var data))
                {
                    logger?.Warning(LogCategory.Persistence,
                        $"[QuestSnapshotMapper] Quest '{snapshot.QuestId}' is not in the fragment library; skipped.");
                    continue;
                }

                registry.Register(Replay(snapshot, data, recorder));
            }
        }

        private static QuestInstance Replay(QuestInstanceSnapshot snapshot, QuestData data,
            IRunProgressionRecorder recorder)
        {
            var quest = new QuestInstance(data, recorder);
            var state = (QuestState)snapshot.State;
            if (state == QuestState.NotStarted)
            {
                return quest;
            }

            quest.Start();
            for (int i = 0; i < snapshot.ObjectiveIds.Count && i < snapshot.ObjectiveCounts.Count; i++)
            {
                if (snapshot.ObjectiveCounts[i] > 0)
                {
                    // Effects are discarded: the facts they wrote are already in the restored store.
                    quest.AdvanceObjective(snapshot.ObjectiveIds[i], snapshot.ObjectiveCounts[i]);
                }
            }

            switch (state)
            {
                case QuestState.Completed:
                    quest.Complete();
                    break;
                case QuestState.Failed:
                    quest.Fail();
                    break;
            }

            if (snapshot.RewardsGranted)
            {
                quest.MarkRewardsGranted();
            }

            return quest;
        }

        private static Dictionary<string, QuestData> IndexQuestData(IFragmentLibrary library)
        {
            var byId = new Dictionary<string, QuestData>(StringComparer.Ordinal);
            foreach (var data in library.FindQuests(null))
            {
                byId[data.QuestId] = data;
            }

            return byId;
        }
    }
}
