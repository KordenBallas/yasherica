namespace LevelGeneration.Journey
{
    /// <summary>
    /// One biome's authored place in the run's escalation: which tier it belongs to (ordering key
    /// only — nothing scales difficulty off it yet), how likely it is within that tier, and how long
    /// a stretch (in planning windows) the run lingers in it. UnityEngine-free; mapped from the
    /// <c>BiomeProgressionConfig</c> SO at install time (CLAUDE.md §7).
    /// </summary>
    public readonly struct BiomeProgressionEntry
    {
        public LevelTheme Theme { get; }
        public int EscalationTier { get; }
        public int SelectionWeight { get; }
        public int StretchMinWindows { get; }
        public int StretchMaxWindows { get; }

        public BiomeProgressionEntry(
            LevelTheme theme, int escalationTier, int selectionWeight,
            int stretchMinWindows, int stretchMaxWindows)
        {
            // Normalize instead of throwing: authored data may be sloppy, and the journey must never
            // take a run down. Weight 0 is meaningful (data-driven exclusion), negatives are not.
            Theme = theme;
            EscalationTier = escalationTier < 1 ? 1 : escalationTier;
            SelectionWeight = selectionWeight < 0 ? 0 : selectionWeight;

            int min = stretchMinWindows < 1 ? 1 : stretchMinWindows;
            int max = stretchMaxWindows < 1 ? 1 : stretchMaxWindows;
            if (max < min)
            {
                (min, max) = (max, min);
            }

            StretchMinWindows = min;
            StretchMaxWindows = max;
        }
    }
}
