namespace World.Sites.Core
{
    /// <summary>
    /// The four base content kinds — the stable spine of the shared world-content vocabulary
    /// (<c>design/world/content-kinds.md</c>). A platform beat is a base kind refined by an open,
    /// data-authored flavor tag (<see cref="ContentBeat"/>); new content types are new flavors,
    /// never new base kinds.
    /// </summary>
    public enum ContentBaseKind
    {
        /// <summary>Connective / traversal breath — no beat.</summary>
        Empty = 0,
        /// <summary>A placed find (an artifact pickup) rolled from the biome loot tables.</summary>
        Loot = 1,
        /// <summary>A placed aggressive enemy — a standalone fight.</summary>
        Combat = 2,
        /// <summary>A placed social (talkable) encounter; quest-bearing/hostile are derived, not kinds.</summary>
        Npc = 3
    }
}
