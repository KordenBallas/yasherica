namespace LevelGeneration.Route
{
    /// <summary>
    /// One midground routing landmark: the world object a feature arc reads as routing around.
    /// Placed at the arc's apex on the far side of the path (away from the camera), off the
    /// platforms — never walkable, never on a hex cell.
    /// </summary>
    public readonly struct LandmarkSpec
    {
        /// <summary>World X of the arc apex the landmark justifies.</summary>
        public readonly float X;

        /// <summary>World Z, always behind the trail (far side, away from the camera).</summary>
        public readonly float Z;

        /// <summary>World Y of the landmark base (slightly embedded under the local tier).</summary>
        public readonly float Y;

        /// <summary>Deterministic pick index into the biome's landmark kit (modulo kit size).</summary>
        public readonly int KitIndex;

        /// <summary>Uniform world scale drawn from the biome's landmark scale range.</summary>
        public readonly float Scale;

        public LandmarkSpec(float x, float z, float y, int kitIndex, float scale)
        {
            X = x;
            Z = z;
            Y = y;
            KitIndex = kitIndex;
            Scale = scale;
        }
    }
}
