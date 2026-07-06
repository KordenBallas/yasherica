namespace World.Dressing.Core
{
    /// <summary>
    /// One planned distant-scatter instance in world space (the run's X axis / depth Z axis —
    /// vertical placement is the view's ortho-compensation business). Pure data.
    /// </summary>
    public readonly struct BackdropScatterPlacement
    {
        public BackdropScatterPlacement(int entryIndex, float x, float z, float yawDegrees, float scale)
        {
            EntryIndex = entryIndex;
            X = x;
            Z = z;
            YawDegrees = yawDegrees;
            Scale = scale;
        }

        /// <summary>Index into the backdrop kit's entry list (modulo on the spawner side).</summary>
        public int EntryIndex { get; }

        /// <summary>World X along the run.</summary>
        public float X { get; }

        /// <summary>World Z — depth behind the traversal field (+Z = away from the camera).</summary>
        public float Z { get; }

        public float YawDegrees { get; }

        public float Scale { get; }
    }
}
