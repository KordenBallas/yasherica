namespace Core.Persistence
{
    /// <summary>
    /// Capture/restore seam for the hero's built body. Implemented on the character-system side
    /// (the restorer that watches the hero rig assemble); the run-state aggregate only speaks this
    /// contract so it stays pure C#.
    /// </summary>
    public interface IHeroBodyPersistence
    {
        /// <summary>The current body as a save section, or null when no hero rig exists yet.</summary>
        HeroBodySnapshot Capture();

        /// <summary>Stages a body to apply onto the hero rig (immediately if it is already
        /// assembled, otherwise as soon as it assembles).</summary>
        void Restore(HeroBodySnapshot snapshot);
    }
}
