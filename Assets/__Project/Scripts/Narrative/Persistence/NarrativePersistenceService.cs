using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;
using Narrative.Generation;
using UnityEngine;

namespace Narrative.Persistence
{
    /// <summary>
    /// Service for saving and loading narrative state.
    /// Orchestrates persistence across quests, NPCs, story progress, and Ink runtime.
    /// Pure C# class with minimal Unity dependencies.
    /// </summary>
    public class NarrativePersistenceService : INarrativePersistenceService
    {
        private const int CURRENT_VERSION = 1;

        private readonly IQuestManager _questManager;
        private readonly IStoryManager _storyManager;
        private readonly IStoryStateProvider _storyStateProvider;
        private readonly IRewardInstanceFactory _rewardInstanceFactory;
        private readonly IReadOnlyList<RewardDefinition> _rewardDefinitions;
        private readonly IReadOnlyList<NpcDefinition> _npcDefinitions;

        public event Action<NarrativeSaveData> OnSaveDataCreated;
        public event Action<NarrativeSaveData> OnSaveDataLoaded;
        public event Action OnStateRestored;

        public int CurrentVersion => CURRENT_VERSION;

        // Tracks NPC relationships across sessions
        private readonly Dictionary<string, NpcRelationshipData> _npcRelationships;

        public NarrativePersistenceService(
            IQuestManager questManager,
            IStoryManager storyManager,
            IStoryStateProvider storyStateProvider = null,
            IRewardInstanceFactory rewardInstanceFactory = null,
            IReadOnlyList<RewardDefinition> rewardDefinitions = null,
            IReadOnlyList<NpcDefinition> npcDefinitions = null)
        {
            _questManager = questManager ?? throw new ArgumentNullException(nameof(questManager));
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
            _storyStateProvider = storyStateProvider;
            _rewardInstanceFactory = rewardInstanceFactory;
            _rewardDefinitions = rewardDefinitions ?? Array.Empty<RewardDefinition>();
            _npcDefinitions = npcDefinitions ?? Array.Empty<NpcDefinition>();
            _npcRelationships = new Dictionary<string, NpcRelationshipData>();
        }

        public NarrativeSaveData CreateSaveData()
        {
            var saveData = new NarrativeSaveData
            {
                Version = CURRENT_VERSION,
                SaveTimestamp = DateTime.UtcNow.Ticks
            };

            // Save story state
            saveData.StoryState = CreateStoryStateSaveData();

            // Save active quests
            foreach (var quest in _questManager.ActiveQuests)
            {
                saveData.ActiveQuests.Add(CreateQuestSaveData(quest));
            }

            // Save completed quests
            foreach (var quest in _questManager.CompletedQuests)
            {
                saveData.CompletedQuests.Add(CreateQuestSaveData(quest));
            }

            // Save NPC relationships
            foreach (var kvp in _npcRelationships)
            {
                saveData.NpcRelationships.Add(kvp.Value);
            }

            // Save current Ink state (if mid-dialogue)
            saveData.CurrentInkState = SaveInkState();

            Debug.Log($"[NarrativePersistenceService] Created save data with {saveData.ActiveQuests.Count} active quests, {saveData.CompletedQuests.Count} completed quests");

            OnSaveDataCreated?.Invoke(saveData);
            return saveData;
        }

        public bool RestoreFromSaveData(NarrativeSaveData saveData)
        {
            if (!ValidateSaveData(saveData))
            {
                Debug.LogWarning("[NarrativePersistenceService] Invalid save data, cannot restore");
                return false;
            }

            try
            {
                // Clear current state
                ClearState();

                // Restore NPC relationships (do this before quests as quests reference NPCs)
                foreach (var relationship in saveData.NpcRelationships)
                {
                    _npcRelationships[relationship.NpcDefinitionId] = relationship;
                }

                // Note: Quest restoration would require additional infrastructure
                // to recreate QuestInstances from save data, including:
                // - Finding the original StoryTemplateDefinition
                // - Recreating BoundStory
                // - Recreating NpcInstances
                // This is intentionally left as a placeholder for full implementation

                // Restore Ink state if present
                if (!string.IsNullOrEmpty(saveData.CurrentInkState))
                {
                    RestoreInkState(saveData.CurrentInkState);
                }

                Debug.Log($"[NarrativePersistenceService] Restored save data with {saveData.NpcRelationships.Count} NPC relationships");

                OnSaveDataLoaded?.Invoke(saveData);
                OnStateRestored?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NarrativePersistenceService] Failed to restore save data: {ex.Message}");
                return false;
            }
        }

        public void ClearState()
        {
            _npcRelationships.Clear();
            Debug.Log("[NarrativePersistenceService] Cleared narrative state");
        }

        public string SaveInkState()
        {
            try
            {
                return _storyManager.SaveState();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NarrativePersistenceService] Failed to save Ink state: {ex.Message}");
                return null;
            }
        }

        public bool RestoreInkState(string inkStateJson)
        {
            if (string.IsNullOrEmpty(inkStateJson))
                return false;

            try
            {
                _storyManager.LoadState(inkStateJson);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NarrativePersistenceService] Failed to restore Ink state: {ex.Message}");
                return false;
            }
        }

        public bool ValidateSaveData(NarrativeSaveData saveData)
        {
            if (saveData == null)
                return false;

            // Check version compatibility
            if (saveData.Version > CURRENT_VERSION)
            {
                Debug.LogWarning($"[NarrativePersistenceService] Save data version {saveData.Version} is newer than current {CURRENT_VERSION}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Updates the relationship score for an NPC.
        /// </summary>
        public void UpdateNpcRelationship(string npcDefinitionId, int delta)
        {
            if (!_npcRelationships.TryGetValue(npcDefinitionId, out var relationship))
            {
                relationship = new NpcRelationshipData
                {
                    NpcDefinitionId = npcDefinitionId,
                    RelationshipScore = 0,
                    HasBeenEncountered = true,
                    CompletedStoryIds = new List<string>()
                };
                _npcRelationships[npcDefinitionId] = relationship;
            }

            relationship.RelationshipScore = Math.Clamp(relationship.RelationshipScore + delta, -100, 100);
            relationship.InteractionCount++;
            relationship.LastInteractionTicks = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Records an NPC encounter.
        /// </summary>
        public void RecordNpcEncounter(string npcDefinitionId)
        {
            if (!_npcRelationships.TryGetValue(npcDefinitionId, out var relationship))
            {
                relationship = new NpcRelationshipData
                {
                    NpcDefinitionId = npcDefinitionId,
                    RelationshipScore = 0,
                    CompletedStoryIds = new List<string>()
                };
                _npcRelationships[npcDefinitionId] = relationship;
            }

            relationship.HasBeenEncountered = true;
            relationship.InteractionCount++;
            relationship.LastInteractionTicks = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Records a story completion for an NPC.
        /// </summary>
        public void RecordNpcStoryCompletion(string npcDefinitionId, string storyId)
        {
            if (!_npcRelationships.TryGetValue(npcDefinitionId, out var relationship))
            {
                relationship = new NpcRelationshipData
                {
                    NpcDefinitionId = npcDefinitionId,
                    RelationshipScore = 0,
                    HasBeenEncountered = true,
                    CompletedStoryIds = new List<string>()
                };
                _npcRelationships[npcDefinitionId] = relationship;
            }

            if (!relationship.CompletedStoryIds.Contains(storyId))
            {
                relationship.CompletedStoryIds.Add(storyId);
            }
        }

        /// <summary>
        /// Gets the relationship data for an NPC.
        /// </summary>
        public NpcRelationshipData GetNpcRelationship(string npcDefinitionId)
        {
            _npcRelationships.TryGetValue(npcDefinitionId, out var relationship);
            return relationship;
        }

        /// <summary>
        /// Gets the relationship score for an NPC.
        /// </summary>
        public int GetNpcRelationshipScore(string npcDefinitionId)
        {
            if (_npcRelationships.TryGetValue(npcDefinitionId, out var relationship))
            {
                return relationship.RelationshipScore;
            }
            return 0;
        }

        private StoryStateSaveData CreateStoryStateSaveData()
        {
            var storyState = new StoryStateSaveData();

            if (_storyStateProvider != null)
            {
                var currentState = _storyStateProvider.CurrentState;
                if (currentState != null)
                {
                    storyState.CompletedStoryIds = new List<string>(currentState.CompletedNodeIds);
                    storyState.VisitedStoryIds = new List<string>(currentState.EncounteredNpcIds);

                    // Extract chapter number from chapter ID if possible
                    if (!string.IsNullOrEmpty(currentState.CurrentChapterId))
                    {
                        if (int.TryParse(currentState.CurrentChapterId.Replace("chapter_", ""), out int chapter))
                        {
                            storyState.CurrentChapter = chapter;
                        }
                    }
                }
            }

            return storyState;
        }

        private QuestSaveData CreateQuestSaveData(QuestInstance quest)
        {
            var saveData = new QuestSaveData
            {
                QuestId = quest.QuestId,
                DisplayName = quest.DisplayName,
                Description = quest.Description,
                SourceTemplateId = quest.BoundStory?.Template?.StoryId,
                Status = quest.Status,
                Outcome = quest.Outcome,
                StartedAtTicks = quest.StartedAt.Ticks,
                CompletedAtTicks = quest.CompletedAt?.Ticks
            };

            // Save objectives
            foreach (var objective in quest.Objectives)
            {
                saveData.Objectives.Add(new QuestObjectiveSaveData
                {
                    ObjectiveId = objective.ObjectiveId,
                    Description = objective.Description,
                    ObjectiveType = objective.ObjectiveType,
                    CurrentProgress = objective.CurrentProgress,
                    RequiredProgress = objective.RequiredProgress,
                    IsRequired = objective.IsRequired,
                    IsComplete = objective.IsComplete
                });
            }

            // Save involved NPC IDs
            foreach (var npc in quest.InvolvedNpcs)
            {
                saveData.InvolvedNpcIds.Add(npc.NpcId);
            }

            // Save reward snapshots
            foreach (var reward in quest.Rewards)
            {
                saveData.Rewards.Add(reward.CreateSnapshot());
            }

            return saveData;
        }

        private RewardDefinition FindRewardDefinition(string rewardId)
        {
            foreach (var def in _rewardDefinitions)
            {
                if (def.RewardId == rewardId)
                    return def;
            }
            return null;
        }

        private NpcDefinition FindNpcDefinition(string npcId)
        {
            foreach (var def in _npcDefinitions)
            {
                if (def.NpcId == npcId)
                    return def;
            }
            return null;
        }
    }
}
