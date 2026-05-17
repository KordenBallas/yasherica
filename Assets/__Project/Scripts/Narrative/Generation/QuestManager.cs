using System;
using System.Collections.Generic;
using Narrative.Dialogue;
using UnityEngine;

namespace Narrative.Generation
{
    /// <summary>
    /// Creates and tracks QuestInstances.
    /// Manages the lifecycle of quests from creation to completion.
    /// Pure C# class - no Unity dependencies except logging.
    /// </summary>
    public class QuestManager : IQuestManager
    {
        private readonly Dictionary<string, QuestInstance> _activeQuests;
        private readonly Dictionary<string, QuestInstance> _completedQuests;
        private readonly Dictionary<string, List<string>> _questsByNpc;
        private readonly IStoryStateProvider _storyStateProvider;
        private int _questCounter;

        public event Action<QuestInstance> OnQuestStarted;
        public event Action<QuestInstance, QuestOutcome> OnQuestCompleted;
        public event Action<QuestInstance, QuestObjective> OnObjectiveUpdated;
        public event Action<QuestInstance> OnQuestFailed;

        public IReadOnlyList<QuestInstance> ActiveQuests => new List<QuestInstance>(_activeQuests.Values);
        public IReadOnlyList<QuestInstance> CompletedQuests => new List<QuestInstance>(_completedQuests.Values);

        /// <summary>
        /// Creates a new quest manager.
        /// </summary>
        public QuestManager(IStoryStateProvider storyStateProvider = null)
        {
            _storyStateProvider = storyStateProvider;
            _activeQuests = new Dictionary<string, QuestInstance>();
            _completedQuests = new Dictionary<string, QuestInstance>();
            _questsByNpc = new Dictionary<string, List<string>>();
            _questCounter = 0;
        }

        public QuestInstance CreateQuest(BoundStory boundStory)
        {
            if (boundStory?.Template == null)
            {
                Debug.LogWarning("[QuestManager] Cannot create quest from null bound story");
                return null;
            }

            // Generate quest ID
            _questCounter++;
            string questId = $"quest_{boundStory.Template.StoryId}_{_questCounter}";

            // Create quest instance
            var quest = new QuestInstance(
                questId,
                boundStory.Template.DisplayName,
                boundStory.Template.Description,
                boundStory);

            // Add involved NPCs
            foreach (var npc in boundStory.BoundNpcs)
            {
                quest.AddInvolvedNpc(npc);
                TrackNpcQuest(npc.NpcId, questId);
            }

            // Add rewards
            foreach (var reward in boundStory.BoundRewards)
            {
                quest.AddReward(reward);
            }

            // Create default objectives based on template
            CreateDefaultObjectives(quest, boundStory);

            // Track the quest
            _activeQuests[questId] = quest;

            Debug.Log($"[QuestManager] Created quest '{quest.DisplayName}' (ID: {questId})");
            return quest;
        }

        public bool StartQuest(string questId)
        {
            if (!_activeQuests.TryGetValue(questId, out var quest))
            {
                Debug.LogWarning($"[QuestManager] Quest not found: {questId}");
                return false;
            }

            if (quest.Status != QuestStatus.NotStarted)
            {
                Debug.LogWarning($"[QuestManager] Quest already started: {questId}");
                return false;
            }

            quest.Start();

            // Notify story state provider
            _storyStateProvider?.StartQuest(questId);

            OnQuestStarted?.Invoke(quest);
            Debug.Log($"[QuestManager] Started quest: {quest.DisplayName}");
            return true;
        }

        public void CompleteQuest(string questId, QuestOutcome outcome)
        {
            if (!_activeQuests.TryGetValue(questId, out var quest))
            {
                Debug.LogWarning($"[QuestManager] Quest not found: {questId}");
                return;
            }

            quest.Complete(outcome);

            // Move to completed
            _activeQuests.Remove(questId);
            _completedQuests[questId] = quest;

            // Update NPC tracking
            foreach (var npc in quest.InvolvedNpcs)
            {
                RemoveNpcQuest(npc.NpcId, questId);
            }

            // Notify story state provider
            _storyStateProvider?.CompleteQuest(questId);

            OnQuestCompleted?.Invoke(quest, outcome);
            Debug.Log($"[QuestManager] Completed quest '{quest.DisplayName}' with outcome: {outcome}");
        }

        public void FailQuest(string questId)
        {
            if (!_activeQuests.TryGetValue(questId, out var quest))
            {
                Debug.LogWarning($"[QuestManager] Quest not found: {questId}");
                return;
            }

            quest.Fail();

            // Move to completed (as failed)
            _activeQuests.Remove(questId);
            _completedQuests[questId] = quest;

            // Update NPC tracking
            foreach (var npc in quest.InvolvedNpcs)
            {
                RemoveNpcQuest(npc.NpcId, questId);
            }

            OnQuestFailed?.Invoke(quest);
            Debug.Log($"[QuestManager] Failed quest: {quest.DisplayName}");
        }

        public void UpdateObjective(string questId, string objectiveId, int progress)
        {
            if (!_activeQuests.TryGetValue(questId, out var quest))
            {
                Debug.LogWarning($"[QuestManager] Quest not found: {questId}");
                return;
            }

            var objective = quest.GetObjective(objectiveId);
            if (objective == null)
            {
                Debug.LogWarning($"[QuestManager] Objective not found: {objectiveId}");
                return;
            }

            quest.UpdateObjective(objectiveId, progress);
            OnObjectiveUpdated?.Invoke(quest, objective);

            // Check for auto-completion
            if (quest.AllObjectivesComplete)
            {
                Debug.Log($"[QuestManager] All objectives complete for quest: {quest.DisplayName}");
            }
        }

        public void CompleteObjective(string questId, string objectiveId)
        {
            if (!_activeQuests.TryGetValue(questId, out var quest))
            {
                Debug.LogWarning($"[QuestManager] Quest not found: {questId}");
                return;
            }

            var objective = quest.GetObjective(objectiveId);
            if (objective == null)
            {
                Debug.LogWarning($"[QuestManager] Objective not found: {objectiveId}");
                return;
            }

            quest.CompleteObjective(objectiveId);
            OnObjectiveUpdated?.Invoke(quest, objective);

            Debug.Log($"[QuestManager] Completed objective '{objective.Description}' in quest '{quest.DisplayName}'");

            // Check for auto-completion
            if (quest.AllObjectivesComplete)
            {
                Debug.Log($"[QuestManager] All objectives complete for quest: {quest.DisplayName}");
            }
        }

        public QuestInstance GetQuest(string questId)
        {
            if (_activeQuests.TryGetValue(questId, out var activeQuest))
                return activeQuest;

            if (_completedQuests.TryGetValue(questId, out var completedQuest))
                return completedQuest;

            return null;
        }

        public bool IsQuestActive(string questId)
        {
            return _activeQuests.ContainsKey(questId);
        }

        public bool IsQuestCompleted(string questId)
        {
            return _completedQuests.ContainsKey(questId);
        }

        public IReadOnlyList<QuestInstance> GetQuestsForNpc(string npcId)
        {
            var quests = new List<QuestInstance>();

            if (_questsByNpc.TryGetValue(npcId, out var questIds))
            {
                foreach (var questId in questIds)
                {
                    var quest = GetQuest(questId);
                    if (quest != null)
                        quests.Add(quest);
                }
            }

            return quests;
        }

        public IReadOnlyList<QuestInstance> GetQuestsForBoundStory(BoundStory boundStory)
        {
            var quests = new List<QuestInstance>();

            if (boundStory?.Template == null)
                return quests;

            var storyId = boundStory.Template.StoryId;

            // Search in active quests
            foreach (var quest in _activeQuests.Values)
            {
                if (quest.BoundStory?.Template?.StoryId == storyId)
                {
                    quests.Add(quest);
                }
            }

            // Also search in completed quests
            foreach (var quest in _completedQuests.Values)
            {
                if (quest.BoundStory?.Template?.StoryId == storyId)
                {
                    quests.Add(quest);
                }
            }

            return quests;
        }

        public void OnDialogueCompleted(BoundStory boundStory, DialogueOutcomeType outcome)
        {
            if (boundStory == null)
                return;

            Debug.Log($"[QuestManager] Processing dialogue completion for story '{boundStory.Template?.DisplayName}' with outcome: {outcome}");

            // Find associated quests
            var associatedQuests = GetQuestsForBoundStory(boundStory);

            foreach (var quest in associatedQuests)
            {
                if (quest.Status != QuestStatus.Active)
                    continue;

                // Complete talk objectives for involved NPCs
                foreach (var npc in quest.InvolvedNpcs)
                {
                    var talkObjective = quest.GetObjectiveByType(ObjectiveType.TalkToNpc);
                    if (talkObjective != null && !talkObjective.IsComplete)
                    {
                        quest.CompleteObjective(talkObjective.ObjectiveId);
                        OnObjectiveUpdated?.Invoke(quest, talkObjective);
                    }
                }

                // Handle outcome-based completion
                switch (outcome)
                {
                    case DialogueOutcomeType.Quest:
                    case DialogueOutcomeType.Continue:
                        // Check if all objectives are complete
                        if (quest.AllObjectivesComplete)
                        {
                            CompleteQuest(quest.QuestId, QuestOutcome.Success);
                        }
                        break;

                    case DialogueOutcomeType.Combat:
                        // Combat outcome - quest continues, completion depends on combat result
                        Debug.Log($"[QuestManager] Quest '{quest.DisplayName}' awaiting combat outcome");
                        break;

                    case DialogueOutcomeType.Exit:
                        // Player exited dialogue - no completion
                        Debug.Log($"[QuestManager] Dialogue exited for quest '{quest.DisplayName}'");
                        break;
                }
            }
        }

        public void EvaluateRewards(QuestInstance quest, QuestOutcome outcome)
        {
            if (quest == null)
            {
                Debug.LogWarning("[QuestManager] Cannot evaluate rewards: quest is null");
                return;
            }

            Debug.Log($"[QuestManager] Evaluating rewards for quest '{quest.DisplayName}' with outcome: {outcome}");

            // Only grant rewards for successful outcomes
            if (outcome != QuestOutcome.Success && outcome != QuestOutcome.PartialSuccess)
            {
                Debug.Log($"[QuestManager] No rewards granted - quest outcome was {outcome}");
                return;
            }

            // Calculate reward multiplier based on outcome
            float rewardMultiplier = outcome == QuestOutcome.Success ? 1.0f : 0.5f;

            foreach (var reward in quest.Rewards)
            {
                // Mark reward as granted
                reward.Claim();

                // Calculate effective amount
                int effectiveAmount = (int)(reward.CalculatedValue * rewardMultiplier);

                Debug.Log($"[QuestManager] Granting reward: {reward.Definition?.DisplayName ?? reward.RewardId} x{effectiveAmount}");

                // Fire reward granted event (to be handled by game systems)
                OnRewardGranted?.Invoke(quest, reward, effectiveAmount);
            }
        }

        /// <summary>
        /// Event fired when a reward is granted.
        /// </summary>
        public event Action<QuestInstance, RewardInstance, int> OnRewardGranted;

        /// <summary>
        /// Gets the total number of active quests.
        /// </summary>
        public int ActiveQuestCount => _activeQuests.Count;

        /// <summary>
        /// Gets the total number of completed quests.
        /// </summary>
        public int CompletedQuestCount => _completedQuests.Count;

        /// <summary>
        /// Clears all quest data (for reset/new game).
        /// </summary>
        public void Clear()
        {
            _activeQuests.Clear();
            _completedQuests.Clear();
            _questsByNpc.Clear();
            _questCounter = 0;
        }

        private void CreateDefaultObjectives(QuestInstance quest, BoundStory boundStory)
        {
            // Create basic talk objective for each NPC
            int objectiveIndex = 0;
            foreach (var npc in boundStory.BoundNpcs)
            {
                objectiveIndex++;
                var objective = new QuestObjective(
                    $"obj_talk_{objectiveIndex}",
                    $"Talk to {npc.DisplayName}",
                    requiredProgress: 1,
                    isRequired: true,
                    objectiveType: ObjectiveType.TalkToNpc);

                quest.AddObjective(objective);
            }

            // Add completion objective
            quest.AddObjective(new QuestObjective(
                "obj_complete",
                "Complete the story",
                requiredProgress: 1,
                isRequired: true,
                objectiveType: ObjectiveType.Custom));
        }

        private void TrackNpcQuest(string npcId, string questId)
        {
            if (!_questsByNpc.ContainsKey(npcId))
            {
                _questsByNpc[npcId] = new List<string>();
            }

            if (!_questsByNpc[npcId].Contains(questId))
            {
                _questsByNpc[npcId].Add(questId);
            }
        }

        private void RemoveNpcQuest(string npcId, string questId)
        {
            if (_questsByNpc.TryGetValue(npcId, out var questIds))
            {
                questIds.Remove(questId);
            }
        }
    }
}
