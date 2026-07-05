namespace LevelGeneration.Journey
{
    /// <summary>
    /// One resolved leg of the run's biome journey: the active biome, its authored escalation tier,
    /// and the half-open window range [FirstWindow, FirstWindow + WindowCount) it covers.
    /// </summary>
    public readonly struct BiomeStretch
    {
        public LevelTheme Theme { get; }
        public int EscalationTier { get; }
        public int StretchIndex { get; }
        public int FirstWindow { get; }
        public int WindowCount { get; }

        public BiomeStretch(LevelTheme theme, int escalationTier, int stretchIndex, int firstWindow, int windowCount)
        {
            Theme = theme;
            EscalationTier = escalationTier;
            StretchIndex = stretchIndex;
            FirstWindow = firstWindow;
            WindowCount = windowCount < 1 ? 1 : windowCount;
        }

        /// <summary>The first window index past this stretch (start of the next stretch).</summary>
        public int EndWindowExclusive => FirstWindow + WindowCount;
    }
}
