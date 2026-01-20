using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Data;
using Narrative.Data.Definitions;
using Narrative.Graph;

namespace Narrative.Providers
{
    /// <summary>
    /// Implementation of NPC story provider service.
    /// Manages NPC-story associations and provides contextual story selection.
    /// Pure C# class - no Unity dependencies (follows MVP pattern).
    /// </summary>
    public class NpcStoryProvider : INpcStoryProvider
    {
        private readonly IReadOnlyList<NpcDefinition> _npcDefinitions;
        private readonly IStoryGraphProvider _storyGraph;
        private readonly Dictionary<string, NpcDefinition> _npcLookup;
        private readonly Dictionary<string, int> _npcEncounterCounts;

        public NpcStoryProvider(
            IReadOnlyList<NpcDefinition> npcDefinitions,
            IStoryGraphProvider storyGraph = null)
        {
            _npcDefinitions = npcDefinitions ?? throw new ArgumentNullException(nameof(npcDefinitions));
            _storyGraph = storyGraph; // Optional - may be null

            _npcLookup = new Dictionary<string, NpcDefinition>();
            _npcEncounterCounts = new Dictionary<string, int>();

            // Build lookup
            foreach (var npc in _npcDefinitions)
            {
                if (!string.IsNullOrEmpty(npc.NpcId))
                {
                    _npcLookup[npc.NpcId] = npc;
                }
            }
        }

        public IReadOnlyList<BaseStoryDefinition> GetStoriesForNpc(string npcId)
        {
            if (string.IsNullOrEmpty(npcId) || !_npcLookup.TryGetValue(npcId, out var npc))
                return Array.Empty<BaseStoryDefinition>();

            if (!npc.HasStoryAssociations)
                return Array.Empty<BaseStoryDefinition>();

            return npc.AssociatedStories
                .Where(a => a.IsValid)
                .Select(a => a.Story)
                .ToList();
        }

        public IReadOnlyList<BaseStoryDefinition> GetAvailableStoriesForNpc(string npcId, StoryState currentState)
        {
            if (currentState == null)
                return Array.Empty<BaseStoryDefinition>();

            var allStories = GetStoriesForNpc(npcId);
            if (allStories.Count == 0)
                return Array.Empty<BaseStoryDefinition>();

            var npc = _npcLookup[npcId];
            var availableStories = new List<BaseStoryDefinition>();

            foreach (var association in npc.AssociatedStories)
            {
                if (!association.IsValid)
                    continue;

                // Check if story is available in the graph
                bool storyAvailable = _storyGraph != null
                    ? _storyGraph.IsStoryAvailable(association.Story, currentState)
                    : true; // If no graph, assume available

                if (!storyAvailable)
                    continue;

                // Check trigger condition
                bool triggerMet = EvaluateTriggerCondition(
                    association.TriggerCondition,
                    npcId,
                    currentState
                );

                if (triggerMet)
                {
                    availableStories.Add(association.Story);
                }
            }

            return availableStories;
        }

        public BaseStoryDefinition GetNextStoryForNpc(string npcId, StoryState currentState)
        {
            var availableStories = GetAvailableStoriesForNpc(npcId, currentState);
            if (availableStories.Count == 0)
                return null;

            // Get the NPC to access priorities
            var npc = _npcLookup[npcId];

            // Score stories by priority and select highest
            var scoredStories = new List<(BaseStoryDefinition story, float score)>();

            foreach (var story in availableStories)
            {
                // Find the association for this story
                var association = npc.AssociatedStories.FirstOrDefault(a => a.Story == story);
                if (association == null)
                    continue;

                float score = CalculateStoryScore(story, association, currentState);
                scoredStories.Add((story, score));
            }

            return scoredStories
                .OrderByDescending(s => s.score)
                .Select(s => s.story)
                .FirstOrDefault();
        }

        public IReadOnlyList<BaseStoryDefinition> GetStoriesByRole(string npcId, NpcStoryRole role)
        {
            if (string.IsNullOrEmpty(npcId) || !_npcLookup.TryGetValue(npcId, out var npc))
                return Array.Empty<BaseStoryDefinition>();

            return npc.AssociatedStories
                .Where(a => a.IsValid && a.Role == role)
                .Select(a => a.Story)
                .ToList();
        }

        public bool HasAvailableStories(string npcId, StoryState currentState)
        {
            return GetAvailableStoriesForNpc(npcId, currentState).Count > 0;
        }

        public NpcDefinition GetNpcDefinition(string npcId)
        {
            return _npcLookup.TryGetValue(npcId, out var npc) ? npc : null;
        }

        public IReadOnlyList<NpcDefinition> GetAllStoryNpcs()
        {
            return _npcDefinitions
                .Where(npc => npc.HasStoryAssociations)
                .ToList();
        }

        /// <summary>
        /// Records an NPC encounter for trigger condition tracking.
        /// </summary>
        public void RecordNpcEncounter(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return;

            if (!_npcEncounterCounts.ContainsKey(npcId))
                _npcEncounterCounts[npcId] = 0;

            _npcEncounterCounts[npcId]++;
        }

        /// <summary>
        /// Gets the encounter count for an NPC.
        /// </summary>
        public int GetNpcEncounterCount(string npcId)
        {
            return _npcEncounterCounts.TryGetValue(npcId, out var count) ? count : 0;
        }

        private bool EvaluateTriggerCondition(
            StoryTriggerCondition condition,
            string npcId,
            StoryState currentState)
        {
            if (condition == null || !condition.HasCondition)
                return true;

            switch (condition.TriggerType)
            {
                case TriggerType.Always:
                    return true;

                case TriggerType.AfterEncounters:
                    int encounterCount = GetNpcEncounterCount(npcId);
                    return encounterCount >= condition.RequiredEncounters;

                case TriggerType.QuestActive:
                    if (string.IsNullOrEmpty(condition.RequiredActiveQuest))
                        return true;
                    return currentState.IsQuestActive(condition.RequiredActiveQuest);

                case TriggerType.QuestCompleted:
                    if (string.IsNullOrEmpty(condition.RequiredCompletedQuest))
                        return true;
                    return currentState.IsQuestCompleted(condition.RequiredCompletedQuest);

                case TriggerType.StoryCompleted:
                    if (string.IsNullOrEmpty(condition.RequiredCompletedStory))
                        return true;
                    return currentState.IsNodeCompleted(condition.RequiredCompletedStory);

                case TriggerType.ChapterReached:
                    // This would require chapter number in state
                    // For now, return true
                    return true;

                case TriggerType.CustomVariable:
                    if (string.IsNullOrEmpty(condition.CustomVariableName))
                        return true;
                    var value = currentState.GetCustomVariable<string>(condition.CustomVariableName);
                    return value == condition.CustomVariableValue;

                default:
                    return false;
            }
        }

        private float CalculateStoryScore(
            BaseStoryDefinition story,
            NpcStoryAssociation association,
            StoryState currentState)
        {
            float score = 0f;

            // Priority from association
            score += association.Priority;

            // Story base priority
            score += story.GetBasePriority() * 0.5f;

            // Role-based scoring
            switch (association.Role)
            {
                case NpcStoryRole.QuestGiver:
                    score += 50f; // High priority for quest givers
                    break;
                case NpcStoryRole.Protagonist:
                    score += 40f;
                    break;
                case NpcStoryRole.Mentor:
                    score += 30f;
                    break;
                case NpcStoryRole.Companion:
                    score += 30f;
                    break;
                case NpcStoryRole.Merchant:
                    score += 20f;
                    break;
                case NpcStoryRole.Witness:
                    score += 15f;
                    break;
                case NpcStoryRole.Antagonist:
                    score += 35f;
                    break;
                case NpcStoryRole.Cameo:
                    score += 10f;
                    break;
                case NpcStoryRole.Bystander:
                    score += 5f;
                    break;
            }

            // Key progression stories get boost
            if (story.PlatformConfig.IsKeyProgression)
            {
                score += 100f;
            }

            // Penalize completed repeatable stories
            if (currentState.IsNodeCompleted(story.StoryId) && !story.CanRepeat())
            {
                score -= 1000f; // Heavily penalize completed non-repeatable stories
            }

            return score;
        }
    }
}
