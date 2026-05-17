using System;
using System.Collections.Generic;
using LevelGeneration;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Runtime narrative context containing current area, available NPCs, and world state.
    /// Pure C# class implementing INarrativeContext.
    /// </summary>
    public class NarrativeContext : INarrativeContext
    {
        private string _currentAreaId;
        private LevelTheme _currentTheme;
        private int _currentChapterNumber;
        private int _currentPlatformIndex;
        private int _totalPlatforms;
        private StoryState _worldState;
        private List<NpcInstance> _availableNpcs;
        private List<StoryTemplateDefinition> _availableTemplates;
        private List<string> _recentStoryIds;
        private PlayerNarrativeData _playerData;
        private int _maxRecentStories;

        public string CurrentAreaId => _currentAreaId;
        public LevelTheme CurrentTheme => _currentTheme;
        public int CurrentChapterNumber => _currentChapterNumber;
        public int CurrentPlatformIndex => _currentPlatformIndex;
        public int TotalPlatforms => _totalPlatforms;

        public float ChapterProgress
        {
            get
            {
                if (_totalPlatforms <= 0)
                    return 0f;
                return _currentPlatformIndex / (float)_totalPlatforms;
            }
        }

        public StoryState WorldState => _worldState;
        public IReadOnlyList<NpcInstance> AvailableNpcs => _availableNpcs;
        public IReadOnlyList<StoryTemplateDefinition> AvailableTemplates => _availableTemplates;
        public IReadOnlyList<string> RecentStoryIds => _recentStoryIds;
        public PlayerNarrativeData PlayerData => _playerData;

        /// <summary>
        /// Creates a new narrative context.
        /// </summary>
        /// <param name="maxRecentStories">Maximum number of recent stories to track</param>
        public NarrativeContext(int maxRecentStories = 10)
        {
            _maxRecentStories = maxRecentStories;
            _currentAreaId = string.Empty;
            _currentTheme = LevelTheme.Forest;
            _currentChapterNumber = 1;
            _currentPlatformIndex = 0;
            _totalPlatforms = 0;
            _worldState = new StoryState();
            _availableNpcs = new List<NpcInstance>();
            _availableTemplates = new List<StoryTemplateDefinition>();
            _recentStoryIds = new List<string>();
            _playerData = new PlayerNarrativeData();
        }

        /// <summary>
        /// Creates a narrative context with initial values.
        /// </summary>
        public NarrativeContext(
            string areaId,
            LevelTheme theme,
            int chapterNumber,
            int totalPlatforms,
            StoryState worldState,
            int maxRecentStories = 10) : this(maxRecentStories)
        {
            _currentAreaId = areaId ?? string.Empty;
            _currentTheme = theme;
            _currentChapterNumber = Math.Max(1, chapterNumber);
            _totalPlatforms = Math.Max(0, totalPlatforms);
            _worldState = worldState ?? new StoryState();
        }

        public void UpdateArea(string areaId, LevelTheme theme, int totalPlatforms)
        {
            _currentAreaId = areaId ?? string.Empty;
            _currentTheme = theme;
            _totalPlatforms = Math.Max(0, totalPlatforms);
            _currentPlatformIndex = 0;
        }

        public void UpdatePlatformIndex(int platformIndex)
        {
            _currentPlatformIndex = Math.Clamp(platformIndex, 0, _totalPlatforms);
        }

        public void UpdateWorldState(StoryState state)
        {
            _worldState = state ?? new StoryState();
        }

        /// <summary>
        /// Sets the current chapter number.
        /// </summary>
        public void SetChapterNumber(int chapterNumber)
        {
            _currentChapterNumber = Math.Max(1, chapterNumber);
        }

        /// <summary>
        /// Sets the available NPCs for this context.
        /// </summary>
        public void SetAvailableNpcs(IEnumerable<NpcInstance> npcs)
        {
            _availableNpcs.Clear();
            if (npcs != null)
            {
                _availableNpcs.AddRange(npcs);
            }
        }

        /// <summary>
        /// Sets the available story templates.
        /// </summary>
        public void SetAvailableTemplates(IEnumerable<StoryTemplateDefinition> templates)
        {
            _availableTemplates.Clear();
            if (templates != null)
            {
                _availableTemplates.AddRange(templates);
            }
        }

        /// <summary>
        /// Sets the player narrative data.
        /// </summary>
        public void SetPlayerData(PlayerNarrativeData data)
        {
            _playerData = data ?? new PlayerNarrativeData();
        }

        public void RecordCompletedStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId))
                return;

            // Remove if already present to move to front
            _recentStoryIds.Remove(storyId);

            // Add to front of list
            _recentStoryIds.Insert(0, storyId);

            // Trim to max size
            while (_recentStoryIds.Count > _maxRecentStories)
            {
                _recentStoryIds.RemoveAt(_recentStoryIds.Count - 1);
            }
        }

        public bool WasRecentlyPlayed(string storyId)
        {
            return _recentStoryIds.Contains(storyId);
        }

        /// <summary>
        /// Populates the narrative context from scenario data and game context.
        /// Sets area ID, theme, platform count, and chapter number.
        /// </summary>
        public void PopulateFromScenario(ScenarioData scenario, GameContext gameContext)
        {
            if (scenario == null)
                return;

            // Update area information
            UpdateArea(
                areaId: scenario.ChapterId ?? Guid.NewGuid().ToString(),
                theme: scenario.Theme,
                totalPlatforms: scenario.EstimatedPlatformCount
            );

            // Set chapter number from game context
            if (gameContext != null)
            {
                SetChapterNumber(gameContext.StoryState);
            }
        }

        /// <summary>
        /// Clears the recent story history.
        /// </summary>
        public void ClearRecentHistory()
        {
            _recentStoryIds.Clear();
        }

        /// <summary>
        /// Updates faction relationship in player data.
        /// </summary>
        public void UpdateFactionRelationship(NpcFaction faction, int delta)
        {
            if (!_playerData.FactionRelationships.ContainsKey(faction))
            {
                _playerData.FactionRelationships[faction] = 0;
            }

            _playerData.FactionRelationships[faction] = Math.Clamp(
                _playerData.FactionRelationships[faction] + delta,
                -100,
                100);
        }

        /// <summary>
        /// Creates a serializable snapshot of this context.
        /// </summary>
        public NarrativeContextSnapshot CreateSnapshot()
        {
            return new NarrativeContextSnapshot
            {
                CurrentAreaId = _currentAreaId,
                CurrentTheme = _currentTheme,
                CurrentChapterNumber = _currentChapterNumber,
                CurrentPlatformIndex = _currentPlatformIndex,
                TotalPlatforms = _totalPlatforms,
                RecentStoryIds = new List<string>(_recentStoryIds),
                PlayerData = _playerData
            };
        }

        /// <summary>
        /// Restores state from a snapshot.
        /// </summary>
        public void RestoreFromSnapshot(NarrativeContextSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            _currentAreaId = snapshot.CurrentAreaId;
            _currentTheme = snapshot.CurrentTheme;
            _currentChapterNumber = snapshot.CurrentChapterNumber;
            _currentPlatformIndex = snapshot.CurrentPlatformIndex;
            _totalPlatforms = snapshot.TotalPlatforms;
            _recentStoryIds = new List<string>(snapshot.RecentStoryIds ?? new List<string>());
            _playerData = snapshot.PlayerData ?? new PlayerNarrativeData();
        }
    }

    /// <summary>
    /// Serializable snapshot of narrative context.
    /// </summary>
    [Serializable]
    public class NarrativeContextSnapshot
    {
        public string CurrentAreaId;
        public LevelTheme CurrentTheme;
        public int CurrentChapterNumber;
        public int CurrentPlatformIndex;
        public int TotalPlatforms;
        public List<string> RecentStoryIds;
        public PlayerNarrativeData PlayerData;
    }
}
