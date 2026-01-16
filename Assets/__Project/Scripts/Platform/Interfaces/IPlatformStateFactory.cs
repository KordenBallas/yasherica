namespace Platform
{
    /// <summary>
    /// Factory interface for creating platform states.
    /// Enables content-driven state selection (Enemy->Combat, Npc->Dialogue, etc.)
    /// </summary>
    public interface IPlatformStateFactory
    {
        /// <summary>
        /// Creates the appropriate active state based on platform content.
        /// </summary>
        IPlatformState CreateActiveState(IPlatform platform);

        /// <summary>
        /// Creates the appropriate idle state based on platform content.
        /// </summary>
        IPlatformState CreateIdleState(IPlatform platform);

        /// <summary>
        /// Creates a completed state for platforms that have finished their content.
        /// </summary>
        IPlatformState CreateCompletedState();

        /// <summary>
        /// Creates a locked state for inaccessible platforms.
        /// </summary>
        IPlatformState CreateLockedState();
    }
}
