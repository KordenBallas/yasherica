namespace Core.Persistence
{
    /// <summary>
    /// Capture/cursor-restore seam for the generated world. Implemented on the level-generation
    /// side. Note the asymmetry: <see cref="RestoreCursors"/> only restores the run-scoped
    /// allocator cursors — the world GEOMETRY is rebuilt by the Area entrypoint through the
    /// streaming coordinator's restored begin, because the world must exist before the hero is
    /// placed, which is a scene-flow concern.
    /// </summary>
    public interface IWorldStatePersistence
    {
        /// <summary>The current world image as a save section, or null when no world exists yet.</summary>
        WorldStateSnapshot Capture();

        /// <summary>Restores the allocator cursors (quest spacing, pending site blocks) from a
        /// loaded save. Null is a no-op.</summary>
        void RestoreCursors(WorldStateSnapshot world);
    }
}
