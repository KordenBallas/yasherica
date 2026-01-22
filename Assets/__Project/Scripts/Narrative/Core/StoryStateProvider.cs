using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Data;
using Narrative.Data.Definitions;
using Narrative.Graph;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// Provides story state management and requirement queries.
    /// Coordinates between IStoryManager and game systems.
    /// Enhanced with story graph integration for dynamic narrative flow.
    /// </summary>
    public class StoryStateProvider : IStoryStateProvider
    {
        private readonly IStoryManager _storyManager;
        private readonly IStoryRequirementParser _requirementParser;
        private readonly IReadOnlyList<StoryChapterDefinition> _chapters;
        private readonly IStoryGraphProvider _storyGraph;
        private readonly IInkExternalFunctionBinder _externalFunctionBinder;

        private StoryState _currentState;
        private StoryChapterDefinition _currentChapter;
        private List<StoryNodeRequirement> _cachedRequirements;

        public event Action<StoryState> OnStateChanged;

        public StoryState CurrentState => _currentState;
        public string CurrentChapterId => _currentState?.CurrentChapterId ?? string.Empty;

        public StoryStateProvider(
            IStoryManager storyManager,
            IStoryRequirementParser requirementParser,
            IReadOnlyList<StoryChapterDefinition> chapters,
            IStoryGraphProvider storyGraph = null,
            IInkExternalFunctionBinder externalFunctionBinder = null)
        {
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
            _requirementParser = requirementParser ?? throw new ArgumentNullException(nameof(requirementParser));
            _chapters = chapters ?? new List<StoryChapterDefinition>();
            _storyGraph = storyGraph; // Optional - may be null if graph not used
            _externalFunctionBinder = externalFunctionBinder; // Optional - may be null

            _currentState = new StoryState();
            _cachedRequirements = new List<StoryNodeRequirement>();

            InitializeFirstChapter();
        }

        private void InitializeFirstChapter()
        {
            if (_chapters.Count == 0)
            {
                Debug.LogWarning("[StoryStateProvider] No chapters available");
                return;
            }

            var firstChapter = _chapters.OrderBy(c => c.ChapterNumber).FirstOrDefault();
            if (firstChapter != null)
            {
                AdvanceToChapter(firstChapter.ChapterId);
            }
        }

        public IReadOnlyList<StoryNodeRequirement> GetCurrentRequirements()
        {
            if (_cachedRequirements.Count == 0 && _currentChapter != null)
            {
                RefreshRequirements();
            }
            return _cachedRequirements;
        }

        public IReadOnlyList<StoryNodeRequirement> GetRequirementsForChapter(string chapterId)
        {
            var chapter = _chapters.FirstOrDefault(c => c.ChapterId == chapterId);
            if (chapter == null)
            {
                Debug.LogWarning($"[StoryStateProvider] Chapter not found: {chapterId}");
                return Array.Empty<StoryNodeRequirement>();
            }

            return _requirementParser.ParseRequirements(chapter);
        }

        public StoryNodeRequirement GetNextStoryNode()
        {
            var requirements = GetCurrentRequirements();
            return requirements
                .Where(r => r.IsKeyNode && !_currentState.IsNodeCompleted(r.NodeId))
                .OrderBy(r => r.Priority)
                .FirstOrDefault();
        }

        public bool ShouldNpcAppear(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return false;

            var requirements = GetCurrentRequirements();
            var npcRequirement = requirements.FirstOrDefault(r => r.NpcId == npcId);

            if (npcRequirement == null)
                return false;

            // Check if prerequisites are met
            foreach (var questId in npcRequirement.RequiredActiveQuests)
            {
                if (!IsQuestActive(questId))
                    return false;
            }

            foreach (var encounteredNpcId in npcRequirement.RequiredEncounteredNpcs)
            {
                if (!_currentState.HasEncounteredNpc(encounteredNpcId))
                    return false;
            }

            return true;
        }

        public string GetNpcDialogueKnot(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return string.Empty;

            var requirements = GetCurrentRequirements();
            var npcRequirement = requirements.FirstOrDefault(r => r.NpcId == npcId);

            return npcRequirement?.InkPath ?? string.Empty;
        }

        public void CompleteStoryNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return;

            _currentState.CompleteNode(nodeId);
            Debug.Log($"[StoryStateProvider] Completed story node: {nodeId}");

            // NEW: Update story graph if available
            _storyGraph?.CompleteStory(nodeId);

            // NEW: Accumulate attributes from completed story
            var story = GetStoryById(nodeId);
            if (story != null && story.Attributes != null && story.Attributes.Count > 0)
            {
                _currentState.AccumulateAttributes(story.Attributes);
                Debug.Log($"[StoryStateProvider] Accumulated {story.Attributes.Count} attributes from story: {nodeId}");
            }

            CheckChapterCompletion();
            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Gets a story definition by ID (searches chapters and side stories).
        /// </summary>
        private BaseStoryDefinition GetStoryById(string storyId)
        {
            // For now, just check chapters
            // In full implementation, this would search all story types
            return _chapters.FirstOrDefault(c => c.ChapterId == storyId);
        }

        public void RecordNpcEncounter(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return;

            _currentState.RecordNpcEncounter(npcId);
            Debug.Log($"[StoryStateProvider] Recorded NPC encounter: {npcId}");
            OnStateChanged?.Invoke(_currentState);
        }

        public void StartQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return;

            _currentState.StartQuest(questId);
            Debug.Log($"[StoryStateProvider] Started quest: {questId}");
            OnStateChanged?.Invoke(_currentState);
        }

        public void CompleteQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return;

            _currentState.CompleteQuest(questId);
            Debug.Log($"[StoryStateProvider] Completed quest: {questId}");
            OnStateChanged?.Invoke(_currentState);
        }

        public bool IsQuestActive(string questId)
        {
            return _currentState.IsQuestActive(questId);
        }

        public bool IsQuestCompleted(string questId)
        {
            return _currentState.IsQuestCompleted(questId);
        }

        public void AdvanceToChapter(string chapterId)
        {
            var chapter = _chapters.FirstOrDefault(c => c.ChapterId == chapterId);
            if (chapter == null)
            {
                Debug.LogError($"[StoryStateProvider] Cannot advance to unknown chapter: {chapterId}");
                return;
            }

            _currentChapter = chapter;
            _currentState.CurrentChapterId = chapterId;

            // Load the chapter's Ink story
            if (chapter.HasInkContent)
            {
                _storyManager.LoadStory(chapter.GetInkJson());
                _externalFunctionBinder?.BindAllExternalFunctions();
                _storyManager.GoToKnot(chapter.StartingKnot);
            }

            RefreshRequirements();
            Debug.Log($"[StoryStateProvider] Advanced to chapter: {chapter.DisplayName}");
            OnStateChanged?.Invoke(_currentState);
        }

        public void SaveState()
        {
            _currentState.InkStateJson = _storyManager.SaveState();
            _currentState.SavedAt = DateTime.UtcNow;
            Debug.Log("[StoryStateProvider] State saved");
        }

        public void LoadState(StoryState state)
        {
            if (state == null)
            {
                Debug.LogWarning("[StoryStateProvider] Cannot load null state");
                return;
            }

            _currentState = state.Clone();

            // Load the chapter
            if (!string.IsNullOrEmpty(_currentState.CurrentChapterId))
            {
                var chapter = _chapters.FirstOrDefault(c => c.ChapterId == _currentState.CurrentChapterId);
                if (chapter != null)
                {
                    _currentChapter = chapter;

                    // Load story and restore Ink state
                    if (chapter.HasInkContent)
                    {
                        _storyManager.LoadStory(chapter.GetInkJson());
                        _externalFunctionBinder?.BindAllExternalFunctions();

                        if (!string.IsNullOrEmpty(_currentState.InkStateJson))
                        {
                            _storyManager.LoadState(_currentState.InkStateJson);
                        }
                    }

                    RefreshRequirements();
                }
            }

            Debug.Log("[StoryStateProvider] State loaded");
            OnStateChanged?.Invoke(_currentState);
        }

        private void RefreshRequirements()
        {
            _cachedRequirements.Clear();

            if (_currentChapter != null)
            {
                var requirements = _requirementParser.ParseRequirements(_currentChapter);
                _cachedRequirements.AddRange(requirements);
            }
        }

        private void CheckChapterCompletion()
        {
            if (_currentChapter == null)
                return;

            // Check if all completion knots have been visited
            bool allCompleted = true;
            foreach (var knot in _currentChapter.CompletionKnots)
            {
                if (_storyManager.GetVisitCount(knot) == 0)
                {
                    allCompleted = false;
                    break;
                }
            }

            if (allCompleted && _currentChapter.NextChapter != null)
            {
                Debug.Log($"[StoryStateProvider] Chapter '{_currentChapter.DisplayName}' completed");
                // Don't auto-advance - let the game decide when to advance
            }
        }
    }
}
