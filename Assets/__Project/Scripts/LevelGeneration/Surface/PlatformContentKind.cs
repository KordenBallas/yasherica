namespace LevelGeneration.Surface
{
    /// <summary>
    /// The content-kind key a platform's size/shape profile is selected by (the four world content
    /// kinds of the density brief, as seen by the area generator). Combat covers everything that can
    /// lead to a fight on this platform: an ambient monster or a story with a required combat slot.
    /// </summary>
    public enum PlatformContentKind
    {
        Empty = 0,
        Loot = 1,
        Combat = 2,
        Npc = 3
    }
}
