namespace Narrative.Director.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free window mechanics for the windowed director. A "window" is the next
    /// <see cref="WindowSize"/> platforms ahead of the player. What fills those platforms is governed by
    /// <see cref="WorldContentDensitySettings"/>, not here — the former narrative-weight and combat
    /// budgets were superseded by the world-content-density model.
    /// Mapped from the <c>RunPacingConfig</c> SO at install time.
    /// </summary>
    public sealed class RunPacingSettings
    {
        public int WindowSize { get; }
        public int LookAheadWindows { get; }

        /// <summary>Ceiling on simultaneously-live threads, ephemeral + arc together (D14/FR7). At
        /// the cap the planner opens no new thread — it waits, never force-drops.</summary>
        public int MaxLiveThreads { get; }

        /// <summary>Lifespan (windows without an advance) for threads whose label has no authored
        /// <c>ThreadDefinition</c> — the implicit ephemeral default (FR2).</summary>
        public int DefaultThreadLifespanWindows { get; }

        /// <summary>Per-run cap on spine reveal-beats the reserved lane may place (D7, P3-1). The
        /// lore-pacing promise is "at most 1–2 reveals per run so each one registers"; 0 disables
        /// the lane entirely.</summary>
        public int MaxSpineRevealsPerRun { get; }

        public RunPacingSettings(int windowSize, int lookAheadWindows,
            int maxLiveThreads = 3, int defaultThreadLifespanWindows = 3,
            int maxSpineRevealsPerRun = 2)
        {
            WindowSize = windowSize < 1 ? 1 : windowSize;
            LookAheadWindows = lookAheadWindows < 1 ? 1 : lookAheadWindows;
            MaxLiveThreads = maxLiveThreads < 1 ? 1 : maxLiveThreads;
            DefaultThreadLifespanWindows = defaultThreadLifespanWindows < 1 ? 1 : defaultThreadLifespanWindows;
            MaxSpineRevealsPerRun = maxSpineRevealsPerRun < 0 ? 0 : maxSpineRevealsPerRun;
        }
    }
}
