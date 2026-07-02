namespace World.Sites.Core
{
    /// <summary>
    /// How a site is pulled into being (content-first, derived from its first anchor beat — no
    /// per-site trigger code): an <see cref="ContentBaseKind.Npc"/> anchor rides the rare quest roll
    /// (Village/City — the settlement exists because of its quest NPC); any other anchor rides the
    /// rare ambient site roll (Camp/Ruin/Lair — the site exists because of its fight or find).
    /// </summary>
    public enum SiteTriggerChannel
    {
        /// <summary>Anchored by an NPC beat — reserved when the density allocator lands a quest slot.</summary>
        Quest = 0,
        /// <summary>Anchored by a combat/loot beat — reserved by the rare, spaced ambient site roll.</summary>
        Ambient = 1
    }
}
