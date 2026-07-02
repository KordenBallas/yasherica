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

        public RunPacingSettings(int windowSize, int lookAheadWindows)
        {
            WindowSize = windowSize < 1 ? 1 : windowSize;
            LookAheadWindows = lookAheadWindows < 1 ? 1 : lookAheadWindows;
        }
    }
}
