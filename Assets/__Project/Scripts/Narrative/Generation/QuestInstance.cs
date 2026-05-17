using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Runtime instance of an active quest with progress and outcomes.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class QuestInstance
    {
        private string _questId;
        private string _displayName;
        private string _description;
        private BoundStory _boundStory;
        private QuestStatus _status;
        private List<QuestObjective> _objectives;
        private List<NpcInstance> _involvedNpcs;
        private List<RewardInstance> _rewards;
        private DateTime _startedAt;
        private DateTime? _completedAt;
        private QuestOutcome _outcome;
        private Dictionary<string, object> _customData;

        /// <summary>
        /// Unique quest identifier.
        /// </summary>
        public string QuestId => _questId;

        /// <summary>
        /// Display name of the quest.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Quest description.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// The bound story this quest was created from.
        /// </summary>
        public BoundStory BoundStory => _boundStory;

        /// <summary>
        /// Current status of the quest.
        /// </summary>
        public QuestStatus Status => _status;

        /// <summary>
        /// Quest objectives.
        /// </summary>
        public IReadOnlyList<QuestObjective> Objectives => _objectives;

        /// <summary>
        /// NPCs involved in this quest.
        /// </summary>
        public IReadOnlyList<NpcInstance> InvolvedNpcs => _involvedNpcs;

        /// <summary>
        /// Potential rewards for this quest.
        /// </summary>
        public IReadOnlyList<RewardInstance> Rewards => _rewards;

        /// <summary>
        /// When the quest was started.
        /// </summary>
        public DateTime StartedAt => _startedAt;

        /// <summary>
        /// When the quest was completed (if applicable).
        /// </summary>
        public DateTime? CompletedAt => _completedAt;

        /// <summary>
        /// The outcome of the quest (if completed).
        /// </summary>
        public QuestOutcome Outcome => _outcome;

        /// <summary>
        /// Whether all objectives are complete.
        /// </summary>
        public bool AllObjectivesComplete
        {
            get
            {
                foreach (var objective in _objectives)
                {
                    if (objective.IsRequired && !objective.IsComplete)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Progress percentage (0-100).
        /// </summary>
        public float Progress
        {
            get
            {
                if (_objectives.Count == 0)
                    return 0f;

                int completedCount = 0;
                foreach (var objective in _objectives)
                {
                    if (objective.IsComplete)
                        completedCount++;
                }
                return (completedCount / (float)_objectives.Count) * 100f;
            }
        }

        /// <summary>
        /// Creates a new quest instance.
        /// </summary>
        public QuestInstance(string questId, string displayName, string description, BoundStory boundStory)
        {
            _questId = questId ?? throw new ArgumentNullException(nameof(questId));
            _displayName = displayName ?? "Unknown Quest";
            _description = description ?? string.Empty;
            _boundStory = boundStory;
            _status = QuestStatus.NotStarted;
            _objectives = new List<QuestObjective>();
            _involvedNpcs = new List<NpcInstance>();
            _rewards = new List<RewardInstance>();
            _customData = new Dictionary<string, object>();
            _outcome = QuestOutcome.Success;
        }

        /// <summary>
        /// Adds an objective to the quest.
        /// </summary>
        public void AddObjective(QuestObjective objective)
        {
            if (objective != null)
            {
                _objectives.Add(objective);
            }
        }

        /// <summary>
        /// Adds an NPC to the quest's involved NPCs.
        /// </summary>
        public void AddInvolvedNpc(NpcInstance npc)
        {
            if (npc != null && !_involvedNpcs.Contains(npc))
            {
                _involvedNpcs.Add(npc);
            }
        }

        /// <summary>
        /// Adds a reward to the quest.
        /// </summary>
        public void AddReward(RewardInstance reward)
        {
            if (reward != null)
            {
                _rewards.Add(reward);
            }
        }

        /// <summary>
        /// Starts the quest.
        /// </summary>
        public void Start()
        {
            if (_status != QuestStatus.NotStarted)
                return;

            _status = QuestStatus.Active;
            _startedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates an objective's progress.
        /// </summary>
        public bool UpdateObjective(string objectiveId, int progress)
        {
            foreach (var objective in _objectives)
            {
                if (objective.ObjectiveId == objectiveId)
                {
                    objective.UpdateProgress(progress);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Completes an objective.
        /// </summary>
        public bool CompleteObjective(string objectiveId)
        {
            foreach (var objective in _objectives)
            {
                if (objective.ObjectiveId == objectiveId)
                {
                    objective.Complete();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Completes the quest with the given outcome.
        /// </summary>
        public void Complete(QuestOutcome outcome)
        {
            if (_status != QuestStatus.Active)
                return;

            _status = QuestStatus.Completed;
            _outcome = outcome;
            _completedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Fails the quest.
        /// </summary>
        public void Fail()
        {
            if (_status != QuestStatus.Active)
                return;

            _status = QuestStatus.Failed;
            _outcome = QuestOutcome.Failure;
            _completedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets custom data for this quest.
        /// </summary>
        public void SetCustomData(string key, object value)
        {
            _customData[key] = value;
        }

        /// <summary>
        /// Gets custom data from this quest.
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (_customData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets an objective by ID.
        /// </summary>
        public QuestObjective GetObjective(string objectiveId)
        {
            foreach (var objective in _objectives)
            {
                if (objective.ObjectiveId == objectiveId)
                    return objective;
            }
            return null;
        }

        /// <summary>
        /// Gets the first objective of a specific type.
        /// </summary>
        public QuestObjective GetObjectiveByType(ObjectiveType type)
        {
            foreach (var objective in _objectives)
            {
                if (objective.ObjectiveType == type)
                    return objective;
            }
            return null;
        }

        /// <summary>
        /// Gets all objectives of a specific type.
        /// </summary>
        public IReadOnlyList<QuestObjective> GetObjectivesByType(ObjectiveType type)
        {
            var results = new List<QuestObjective>();
            foreach (var objective in _objectives)
            {
                if (objective.ObjectiveType == type)
                    results.Add(objective);
            }
            return results;
        }
    }

    /// <summary>
    /// Status of a quest.
    /// </summary>
    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed,
        Abandoned
    }

    /// <summary>
    /// Represents a quest objective.
    /// </summary>
    public class QuestObjective
    {
        private string _objectiveId;
        private string _description;
        private int _currentProgress;
        private int _requiredProgress;
        private bool _isRequired;
        private bool _isComplete;
        private ObjectiveType _objectiveType;

        public string ObjectiveId => _objectiveId;
        public string Description => _description;
        public int CurrentProgress => _currentProgress;
        public int RequiredProgress => _requiredProgress;
        public bool IsRequired => _isRequired;
        public bool IsComplete => _isComplete;
        public ObjectiveType ObjectiveType => _objectiveType;

        public float ProgressPercent => _requiredProgress > 0
            ? (_currentProgress / (float)_requiredProgress) * 100f
            : 0f;

        public QuestObjective(
            string objectiveId,
            string description,
            int requiredProgress = 1,
            bool isRequired = true,
            ObjectiveType objectiveType = ObjectiveType.Custom)
        {
            _objectiveId = objectiveId;
            _description = description;
            _requiredProgress = Math.Max(1, requiredProgress);
            _isRequired = isRequired;
            _objectiveType = objectiveType;
            _currentProgress = 0;
            _isComplete = false;
        }

        public void UpdateProgress(int progress)
        {
            _currentProgress = Math.Max(0, progress);
            if (_currentProgress >= _requiredProgress)
            {
                _isComplete = true;
            }
        }

        public void IncrementProgress(int amount = 1)
        {
            UpdateProgress(_currentProgress + amount);
        }

        public void Complete()
        {
            _currentProgress = _requiredProgress;
            _isComplete = true;
        }
    }

    /// <summary>
    /// Type of quest objective.
    /// </summary>
    public enum ObjectiveType
    {
        Custom,
        TalkToNpc,
        DefeatEnemy,
        CollectItem,
        ReachLocation,
        EscortNpc,
        Survive,
        Investigate
    }
}
