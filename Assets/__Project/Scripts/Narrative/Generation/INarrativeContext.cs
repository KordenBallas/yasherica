using System.Collections.Generic;
using LevelGeneration;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    // Forward declaration for GameContext parameter
    using GameContext = LevelGeneration.GameContext;

    /// <summary>
    /// Interface for providing current narrative environment context.
    /// Aggregates area, NPCs, world state for story generation decisions.
    /// </summary>
    public interface INarrativeContext
    {
        /// <summary>
        /// The current area/location ID.
        /// </summary>
        string CurrentAreaId { get; }

        /// <summary>
        /// The current level theme.
        /// </summary>
        LevelTheme CurrentTheme { get; }

        /// <summary>
        /// The current chapter number.
        /// </summary>
        int CurrentChapterNumber { get; }

        /// <summary>
        /// The current platform index within the area.
        /// </summary>
        int CurrentPlatformIndex { get; }

        /// <summary>
        /// Total platforms in the current area.
        /// </summary>
        int TotalPlatforms { get; }

        /// <summary>
        /// Progress through current chapter (0.0 - 1.0).
        /// </summary>
        float ChapterProgress { get; }

        /// <summary>
        /// The current story state.
        /// </summary>
        StoryState WorldState { get; }

        /// <summary>
        /// NPCs available in the current area.
        /// </summary>
        IReadOnlyList<NpcInstance> AvailableNpcs { get; }

        /// <summary>
        /// Story templates available for selection.
        /// </summary>
        IReadOnlyList<StoryTemplateDefinition> AvailableTemplates { get; }

        /// <summary>
        /// Recently completed story IDs (for variety).
        /// </summary>
        IReadOnlyList<string> RecentStoryIds { get; }

        /// <summary>
        /// Player-specific data affecting narrative.
        /// </summary>
        PlayerNarrativeData PlayerData { get; }

        /// <summary>
        /// Updates the context with new area information.
        /// </summary>
        /// <param name="areaId">New area ID</param>
        /// <param name="theme">New theme</param>
        /// <param name="totalPlatforms">Total platforms in area</param>
        void UpdateArea(string areaId, LevelTheme theme, int totalPlatforms);

        /// <summary>
        /// Updates the current platform index.
        /// </summary>
        /// <param name="platformIndex">New platform index</param>
        void UpdatePlatformIndex(int platformIndex);

        /// <summary>
        /// Updates the world state reference.
        /// </summary>
        /// <param name="state">New world state</param>
        void UpdateWorldState(StoryState state);

        /// <summary>
        /// Adds a story ID to recent history.
        /// </summary>
        /// <param name="storyId">Story ID to record</param>
        void RecordCompletedStory(string storyId);

        /// <summary>
        /// Checks if a story was recently played.
        /// </summary>
        /// <param name="storyId">Story ID to check</param>
        /// <returns>True if the story is in recent history</returns>
        bool WasRecentlyPlayed(string storyId);

        /// <summary>
        /// Populates the narrative context from scenario data and game context.
        /// Sets area ID, theme, platform count, and chapter number.
        /// </summary>
        /// <param name="scenario">The scenario data containing area information</param>
        /// <param name="gameContext">The game context containing player state</param>
        void PopulateFromScenario(ScenarioData scenario, GameContext gameContext);
    }

    /// <summary>
    /// Player-specific data that affects narrative choices.
    /// </summary>
    public class PlayerNarrativeData
    {
        /// <summary>
        /// Player's preferred story themes.
        /// </summary>
        public List<string> PreferredThemes { get; set; } = new();

        /// <summary>
        /// Player's karma/alignment score (-100 to 100).
        /// </summary>
        public int AlignmentScore { get; set; }

        /// <summary>
        /// Relationship scores with factions.
        /// </summary>
        public Dictionary<NpcFaction, int> FactionRelationships { get; set; } = new();

        /// <summary>
        /// Total quests completed.
        /// </summary>
        public int TotalQuestsCompleted { get; set; }

        /// <summary>
        /// Combat encounters won.
        /// </summary>
        public int CombatVictories { get; set; }

        /// <summary>
        /// Peaceful resolutions chosen.
        /// </summary>
        public int PeacefulResolutions { get; set; }
    }
}
