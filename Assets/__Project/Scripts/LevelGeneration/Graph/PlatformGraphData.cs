using System.Collections.Generic;
using Platform;

namespace LevelGeneration
{
    public class PlatformGraphData
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
        public int EntryNodeId { get; set; }  // Node with no incoming edges (entry platform)
    }

    public class GraphNode
    {
        public int Id { get; set; }
        public PlatformType Type { get; set; }
        public List<PlatformContentType> ContentTypes { get; set; } = new();  // Multiple content types
        public bool IsKeyPlatform { get; set; }  // vs filler
        public StoryPlatformData StoryData { get; set; }

        /// <summary>
        /// Content instances the planner already built (e.g. a narrative <c>NpcContent</c> carrying a
        /// minted actor + planned story). When present, the area generator adds these instead of creating
        /// content from <see cref="ContentTypes"/>.
        /// </summary>
        public List<IPlatformContent> PrebuiltContent { get; set; }
    }

    public class GraphEdge
    {
        public int FromNodeId { get; set; }
        public int ToNodeId { get; set; }
    }

    public enum PlatformType
    {
        Simple,
        Combat
    }

    public enum PlatformContentType
    {
        None,
        Enemy,
        Npc,
        Loot,
        Quest,
        Village,
        Crossroad,
        Dialogue,
        Cutscene
    }
}

