namespace Narrative.Director.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free pacing budget for the windowed director (P2). A "window" is the next
    /// <see cref="WindowSize"/> platforms ahead of the player; the planner fills it from eligible stories
    /// up to <see cref="NarrativeBudgetPerWindow"/> (sum of story weights) and keeps combat-bearing
    /// encounters within [<see cref="MinCombatPerWindow"/>, <see cref="MaxCombatPerWindow"/>] — a budget
    /// dimension tracked separately from narrative weight.
    /// Mapped from the <c>RunPacingConfig</c> SO at install time.
    /// </summary>
    public sealed class RunPacingSettings
    {
        public int WindowSize { get; }
        public int NarrativeBudgetPerWindow { get; }
        public int MinCombatPerWindow { get; }
        public int MaxCombatPerWindow { get; }
        public int LookAheadWindows { get; }

        public RunPacingSettings(int windowSize, int narrativeBudgetPerWindow, int minCombatPerWindow,
            int maxCombatPerWindow, int lookAheadWindows)
        {
            WindowSize = windowSize < 1 ? 1 : windowSize;
            NarrativeBudgetPerWindow = narrativeBudgetPerWindow < 0 ? 0 : narrativeBudgetPerWindow;
            MinCombatPerWindow = minCombatPerWindow < 0 ? 0 : minCombatPerWindow;
            MaxCombatPerWindow = maxCombatPerWindow < MinCombatPerWindow ? MinCombatPerWindow : maxCombatPerWindow;
            LookAheadWindows = lookAheadWindows < 1 ? 1 : lookAheadWindows;
        }
    }
}
