using System.Collections.Generic;

namespace LevelGeneration
{
    public class ScenarioData
    {
        public List<PlatformRequirement> RequiredPlatforms { get; set; } = new();
        public int DifficultyLevel { get; set; }
        public LevelTheme Theme { get; set; }
        public int EstimatedPlatformCount { get; set; }
    }

    public class PlatformRequirement
    {
        public PlatformType Type { get; set; }
        public List<PlatformContentType> ContentTypes { get; set; } = new();
    }

    public enum LevelTheme
    {
        Forest,
        Desert,
        Mountain,
        Cave
    }
}

