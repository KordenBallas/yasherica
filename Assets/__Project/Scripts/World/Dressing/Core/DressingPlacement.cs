namespace World.Dressing.Core
{
    /// <summary>
    /// One planned dressing instance in platform-local space. Pure data: the spawner resolves
    /// (Role, EntryIndex) to a kit prefab and instantiates it under the platform transform —
    /// every placement is platform-local by construction, so dressing can never cross a gap.
    /// </summary>
    public readonly struct DressingPlacement
    {
        public DressingPlacement(
            DressingRole role, int entryIndex, float localX, float localZ, float yawDegrees, float scale)
        {
            Role = role;
            EntryIndex = entryIndex;
            LocalX = localX;
            LocalZ = localZ;
            YawDegrees = yawDegrees;
            Scale = scale;
        }

        public DressingRole Role { get; }

        /// <summary>Index into the kit's list for <see cref="Role"/> (modulo on the spawner side).</summary>
        public int EntryIndex { get; }

        public float LocalX { get; }

        public float LocalZ { get; }

        public float YawDegrees { get; }

        public float Scale { get; }
    }
}
