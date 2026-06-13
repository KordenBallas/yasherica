namespace CharacterSystem.Core
{
    /// <summary>
    /// Distinguishes sockets that always exist on the skeleton (Tier-1)
    /// from sockets contributed by an equipped part (Tier-2).
    /// </summary>
    public enum SocketTier
    {
        Skeleton,
        Part
    }
}
