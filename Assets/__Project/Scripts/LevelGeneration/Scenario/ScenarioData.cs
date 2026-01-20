using System.Collections.Generic;
using Narrative;

namespace LevelGeneration
{
    public class ScenarioData
    {
        public List<PlatformRequirement> RequiredPlatforms { get; set; } = new();
        public int DifficultyLevel { get; set; }
        public LevelTheme Theme { get; set; }
        public int EstimatedPlatformCount { get; set; }

        /// <summary>
        /// Current story chapter ID for this scenario.
        /// </summary>
        public string ChapterId { get; set; }
    }

    public class PlatformRequirement
    {
        public PlatformType Type { get; set; }
        public List<PlatformContentType> ContentTypes { get; set; } = new();

        /// <summary>
        /// Story-specific data for this platform (NPCs, dialogue, etc.).
        /// Null for procedurally generated filler platforms.
        /// </summary>
        public StoryPlatformData StoryData { get; set; }

        /// <summary>
        /// Whether this is a key story platform (required for progression).
        /// </summary>
        public bool IsKeyPlatform => StoryData?.IsKeyNode ?? false;
    }

    /// <summary>
    /// Story-specific data for a platform, derived from Ink content.
    /// </summary>
    public class StoryPlatformData
    {
        /// <summary>
        /// The story node requirement this platform fulfills.
        /// </summary>
        public StoryNodeRequirement Requirement { get; set; }

        /// <summary>
        /// NPC ID to spawn on this platform.
        /// </summary>
        public string NpcId { get; set; }

        /// <summary>
        /// Ink knot for dialogue on this platform.
        /// </summary>
        public string DialogueKnot { get; set; }

        /// <summary>
        /// Whether the NPC can become an enemy.
        /// </summary>
        public bool NpcCanBecomeEnemy { get; set; }

        /// <summary>
        /// Enemy ID if NPC transitions to combat.
        /// </summary>
        public string EnemyId { get; set; }

        /// <summary>
        /// Whether this is a key story node.
        /// </summary>
        public bool IsKeyNode { get; set; }

        /// <summary>
        /// Priority for platform placement (lower = earlier).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Side story ID if this platform is for a side story.
        /// Null/empty for main story platforms.
        /// </summary>
        public string SideStoryId { get; set; }

        /// <summary>
        /// Checks if this platform is for a side story.
        /// </summary>
        public bool IsSideStory => !string.IsNullOrEmpty(SideStoryId);

        public StoryPlatformData()
        {
        }

        public StoryPlatformData(StoryNodeRequirement requirement)
        {
            Requirement = requirement;
            NpcId = requirement.NpcId;
            DialogueKnot = requirement.InkPath;
            NpcCanBecomeEnemy = requirement.NpcCanBecomeEnemy;
            EnemyId = requirement.EnemyId;
            IsKeyNode = requirement.IsKeyNode;
            Priority = requirement.Priority;
        }
    }

    public enum LevelTheme
    {
        Forest,
        Desert,
        Mountain,
        Cave
    }
}

