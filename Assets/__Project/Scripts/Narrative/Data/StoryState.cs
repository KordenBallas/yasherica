using System;
using System.Collections.Generic;
using System.Linq;

namespace Narrative
{
    /// <summary>
    /// Serializable story progress data.
    /// Used for saving/loading story state independent of Ink runtime.
    /// </summary>
    [Serializable]
    public class StoryState
    {
        /// <summary>
        /// The raw Ink state JSON for exact story position restoration.
        /// </summary>
        public string InkStateJson { get; set; }

        /// <summary>
        /// Current chapter identifier for game-level tracking.
        /// </summary>
        public string CurrentChapterId { get; set; }

        /// <summary>
        /// IDs of completed story nodes (quests, events, etc.).
        /// </summary>
        public List<string> CompletedNodeIds { get; set; } = new();

        /// <summary>
        /// IDs of NPCs the player has encountered.
        /// </summary>
        public List<string> EncounteredNpcIds { get; set; } = new();

        /// <summary>
        /// Active quest IDs.
        /// </summary>
        public List<string> ActiveQuestIds { get; set; } = new();

        /// <summary>
        /// Completed quest IDs.
        /// </summary>
        public List<string> CompletedQuestIds { get; set; } = new();

        /// <summary>
        /// Custom story variables for game-specific tracking.
        /// </summary>
        public Dictionary<string, object> CustomVariables { get; set; } = new();

        /// <summary>
        /// Accumulated story attributes from completed stories.
        /// Used for dynamic story connections and thematic continuity.
        /// </summary>
        public List<Data.StoryAttribute> AccumulatedAttributes { get; set; } = new();

        /// <summary>
        /// Timestamp when this state was saved.
        /// </summary>
        public DateTime SavedAt { get; set; }

        public StoryState()
        {
            SavedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks a story node as completed.
        /// </summary>
        public void CompleteNode(string nodeId)
        {
            if (!CompletedNodeIds.Contains(nodeId))
            {
                CompletedNodeIds.Add(nodeId);
            }
        }

        /// <summary>
        /// Checks if a story node has been completed.
        /// </summary>
        public bool IsNodeCompleted(string nodeId)
        {
            return CompletedNodeIds.Contains(nodeId);
        }

        /// <summary>
        /// Records an NPC encounter.
        /// </summary>
        public void RecordNpcEncounter(string npcId)
        {
            if (!EncounteredNpcIds.Contains(npcId))
            {
                EncounteredNpcIds.Add(npcId);
            }
        }

        /// <summary>
        /// Checks if an NPC has been encountered.
        /// </summary>
        public bool HasEncounteredNpc(string npcId)
        {
            return EncounteredNpcIds.Contains(npcId);
        }

        /// <summary>
        /// Starts a quest.
        /// </summary>
        public void StartQuest(string questId)
        {
            if (!ActiveQuestIds.Contains(questId) && !CompletedQuestIds.Contains(questId))
            {
                ActiveQuestIds.Add(questId);
            }
        }

        /// <summary>
        /// Completes a quest.
        /// </summary>
        public void CompleteQuest(string questId)
        {
            ActiveQuestIds.Remove(questId);
            if (!CompletedQuestIds.Contains(questId))
            {
                CompletedQuestIds.Add(questId);
            }
        }

        /// <summary>
        /// Checks if a quest is active.
        /// </summary>
        public bool IsQuestActive(string questId)
        {
            return ActiveQuestIds.Contains(questId);
        }

        /// <summary>
        /// Checks if a quest is completed.
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            return CompletedQuestIds.Contains(questId);
        }

        /// <summary>
        /// Sets a custom variable.
        /// </summary>
        public void SetCustomVariable(string key, object value)
        {
            CustomVariables[key] = value;
        }

        /// <summary>
        /// Gets a custom variable.
        /// </summary>
        public T GetCustomVariable<T>(string key, T defaultValue = default)
        {
            if (CustomVariables.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Accumulates attributes from a completed story.
        /// </summary>
        public void AccumulateAttributes(IReadOnlyList<Data.StoryAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0)
                return;

            foreach (var attribute in attributes)
            {
                // Only accumulate if not already present (avoid duplicates)
                bool exists = AccumulatedAttributes.Any(a =>
                    a.AttributeKey == attribute.AttributeKey &&
                    a.AttributeValue == attribute.AttributeValue);

                if (!exists)
                {
                    AccumulatedAttributes.Add(attribute);
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of this state.
        /// </summary>
        public StoryState Clone()
        {
            return new StoryState
            {
                InkStateJson = InkStateJson,
                CurrentChapterId = CurrentChapterId,
                CompletedNodeIds = new List<string>(CompletedNodeIds),
                EncounteredNpcIds = new List<string>(EncounteredNpcIds),
                ActiveQuestIds = new List<string>(ActiveQuestIds),
                CompletedQuestIds = new List<string>(CompletedQuestIds),
                CustomVariables = new Dictionary<string, object>(CustomVariables),
                AccumulatedAttributes = new List<Data.StoryAttribute>(AccumulatedAttributes),
                SavedAt = SavedAt
            };
        }
    }
}
