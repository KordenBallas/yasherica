using Narrative.Director.Core;

namespace Narrative.Director.Data
{
    /// <summary>
    /// The only bridge from the <see cref="RunPacingConfig"/> SO to the UnityEngine-free
    /// <see cref="RunPacingSettings"/> Core record (CLAUDE.md §7). Falls back to sane defaults when no
    /// config asset is wired.
    /// </summary>
    public static class RunPacingConfigMapper
    {
        public static RunPacingSettings ToSettings(RunPacingConfig config)
        {
            if (config == null)
            {
                return new RunPacingSettings(windowSize: 4, lookAheadWindows: 1);
            }

            return new RunPacingSettings(config.WindowSize, config.LookAheadWindows);
        }
    }
}
