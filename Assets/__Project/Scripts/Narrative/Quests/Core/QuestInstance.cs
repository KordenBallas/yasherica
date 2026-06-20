using System.Collections.Generic;
using CharacterProgression.Core;
using Narrative.Facts.Core;

namespace Narrative.Quests.Core
{
    public enum QuestState
    {
        NotStarted = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }

    /// <summary>
    /// Per-run mutable quest state over an immutable <see cref="QuestData"/> (R2). Drives the
    /// lifecycle (Start → advance objectives → Complete/Fail) and returns the fact effects to apply
    /// at each transition (the caller applies them through the applier against this quest's own
    /// <see cref="QuestData.Footprint"/>, W2-1). Bridges to the legacy <see cref="IRunProgressionRecorder"/>
    /// so existing run-state gating stays current during the transition.
    /// </summary>
    public sealed class QuestInstance
    {
        private readonly QuestData _data;
        private readonly IRunProgressionRecorder _recorder;
        private readonly Dictionary<string, int> _progress = new Dictionary<string, int>();
        private readonly HashSet<string> _completedObjectives = new HashSet<string>();

        public QuestState State { get; private set; } = QuestState.NotStarted;
        public QuestData Data => _data;
        public IReadOnlyList<FactKeyShapeCore> Footprint => _data.Footprint;

        public QuestInstance(QuestData data, IRunProgressionRecorder recorder = null)
        {
            _data = data;
            _recorder = recorder;
        }

        public void Start()
        {
            if (State != QuestState.NotStarted)
            {
                return;
            }

            State = QuestState.Active;
            _recorder?.StartQuest(_data.QuestId);
        }

        /// <summary>
        /// Advances an objective's count. When it reaches the target the objective completes and its
        /// completion effects are returned for the caller to apply; otherwise an empty list.
        /// </summary>
        public IReadOnlyList<FactEffectCore> AdvanceObjective(string objectiveId, int amount = 1)
        {
            if (State != QuestState.Active)
            {
                return System.Array.Empty<FactEffectCore>();
            }

            var objective = FindObjective(objectiveId);
            if (objective == null || _completedObjectives.Contains(objectiveId))
            {
                return System.Array.Empty<FactEffectCore>();
            }

            _progress.TryGetValue(objectiveId, out var current);
            current += amount;
            _progress[objectiveId] = current;

            if (current >= objective.TargetCount)
            {
                _completedObjectives.Add(objectiveId);
                return objective.CompletionEffects;
            }

            return System.Array.Empty<FactEffectCore>();
        }

        public bool IsObjectiveComplete(string objectiveId) => _completedObjectives.Contains(objectiveId);

        public bool AllObjectivesComplete => _completedObjectives.Count >= _data.Objectives.Count;

        public int ProgressOf(string objectiveId) => _progress.TryGetValue(objectiveId, out var c) ? c : 0;

        /// <summary>Transitions to Completed and returns the on-complete effects.</summary>
        public IReadOnlyList<FactEffectCore> Complete()
        {
            if (State == QuestState.Completed)
            {
                return System.Array.Empty<FactEffectCore>();
            }

            State = QuestState.Completed;
            _recorder?.CompleteQuest(_data.QuestId);
            return _data.OnCompleteEffects;
        }

        /// <summary>Transitions to Failed and returns the on-fail effects.</summary>
        public IReadOnlyList<FactEffectCore> Fail()
        {
            if (State == QuestState.Failed)
            {
                return System.Array.Empty<FactEffectCore>();
            }

            State = QuestState.Failed;
            _recorder?.FailQuest(_data.QuestId);
            return _data.OnFailEffects;
        }

        private QuestObjective FindObjective(string objectiveId)
        {
            for (int i = 0; i < _data.Objectives.Count; i++)
            {
                if (_data.Objectives[i].ObjectiveId == objectiveId)
                {
                    return _data.Objectives[i];
                }
            }

            return null;
        }
    }
}
