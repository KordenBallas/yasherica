using System.Collections.Generic;
using UnityEngine;
using LevelGeneration;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// Defines a story chapter with metadata and required content.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryChapterDefinition", menuName = "Narrative/Story/Chapter")]
    public class StoryChapterDefinition : BaseStoryDefinition
    {
        [Header("Chapter-Specific")]
        [Tooltip("Chapter number for ordering")]
        [SerializeField] private int _chapterNumber = 1;

        [Header("Theme & Environment")]
        [Tooltip("Visual theme for platforms in this chapter")]
        [SerializeField] private LevelTheme _theme = LevelTheme.Forest;

        [Tooltip("Base difficulty level for this chapter")]
        [SerializeField] private int _baseDifficulty = 1;

        [Header("Required NPCs")]
        [Tooltip("NPCs that must appear in this chapter")]
        [SerializeField] private List<NpcDefinition> _requiredNpcs = new();

        [Header("Dialogue Sessions")]
        [Tooltip("Key dialogue sessions in this chapter")]
        [SerializeField] private List<DialogueSessionDefinition> _keyDialogueSessions = new();

        [Header("Progression")]
        [Tooltip("Knots that mark chapter completion")]
        [SerializeField] private List<string> _completionKnots = new();

        [Tooltip("Chapter unlocked after completing this one (legacy - use Relationships instead)")]
        [SerializeField] private StoryChapterDefinition _nextChapter;

        // Public read-only accessors - base properties inherited from BaseStoryDefinition
        // _storyId, _displayName, _description, _inkJsonAsset, _startingKnot are inherited

        // Chapter-specific accessors
        public string ChapterId => _storyId; // Alias for backward compatibility
        public int ChapterNumber => _chapterNumber;
        public LevelTheme Theme => _theme;
        public int BaseDifficulty => _baseDifficulty;
        public IReadOnlyList<NpcDefinition> RequiredNpcs => _requiredNpcs;
        public IReadOnlyList<DialogueSessionDefinition> KeyDialogueSessions => _keyDialogueSessions;
        public IReadOnlyList<string> CompletionKnots => _completionKnots;
        public StoryChapterDefinition NextChapter => _nextChapter;

        /// <summary>
        /// Chapters are always on the main story path.
        /// </summary>
        public override StoryPlatformType GetPlatformType()
        {
            return StoryPlatformType.Cutscene; // Chapters typically use cutscenes for story delivery
        }

        /// <summary>
        /// Chapters cannot be repeated.
        /// </summary>
        public override bool CanRepeat()
        {
            return false;
        }

        /// <summary>
        /// Gets the theme for this chapter.
        /// </summary>
        public override LevelTheme? GetTheme()
        {
            return _theme;
        }

        private void OnValidate()
        {
            // Auto-set StoryType for chapters
            _storyType = StoryType.Chapter;
        }
    }
}
