namespace Core.Logging
{
    /// <summary>
    /// Identifies which game system a log line belongs to, so logging can be
    /// muted or raised per system at runtime via <see cref="LoggingConfig"/>.
    /// Add a value here when a new system starts logging; map it in the config asset.
    /// </summary>
    public enum LogCategory
    {
        /// <summary>Default bucket for logs that have not been assigned a system yet.</summary>
        General = 0,
        Core,
        Camera,
        Area,
        Platform,
        LevelGeneration,
        Combat,
        Character,
        CharacterSystem,
        Narrative,
        Dialogue,
        Inventory,
        Loot,
        Mutation,
        Persistence,

        /// <summary>Shared UI infrastructure (popovers, previews) that belongs to no one system.</summary>
        UI
    }
}
