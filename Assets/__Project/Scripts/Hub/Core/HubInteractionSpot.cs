namespace Hub.Core
{
    /// <summary>
    /// One F-interactable spot on the Hub platform (O1 rework): a planar position with a small
    /// interaction radius. The junk-keeper NPC and the three biome portals are all spots; what an
    /// interaction *does* is mapped by the proximity presenter, not carried here. UnityEngine-free.
    /// </summary>
    public sealed class HubInteractionSpot
    {
        public HubInteractionSpot(string id, float x, float z, float radius)
        {
            Id = id ?? string.Empty;
            X = x;
            Z = z;
            Radius = radius > 0f ? radius : 0f;
        }

        public string Id { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
    }
}
