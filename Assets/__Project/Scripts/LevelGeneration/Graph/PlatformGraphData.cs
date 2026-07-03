using System.Collections.Generic;
using Platform;
using World.Sites.Core;

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

        /// <summary>
        /// Site membership stamped by the planner (world-sites brief); <see cref="SiteStamp.Wild"/>
        /// outside a site block. The M5 site-dressing pass reads this to dress a block as one place.
        /// </summary>
        public SiteStamp Site { get; set; }

        /// <summary>
        /// The beat's <c>base·flavor</c> refinement (e.g. "market" on a Loot node); empty when
        /// unflavored. Flows into the loot roll context as a bias tag.
        /// </summary>
        public string ContentFlavor { get; set; }
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

