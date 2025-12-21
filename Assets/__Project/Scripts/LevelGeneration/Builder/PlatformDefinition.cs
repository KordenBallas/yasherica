using System.Collections.Generic;

namespace LevelGeneration
{
    public class PlatformDefinition
    {
        public int Id { get; set; }
        public PlatformType Type { get; set; }
        public List<PlatformContentType> ContentTypes { get; set; } = new();
        // ... other metadata for platform creation
    }
}

