using Core.Logging;
using Loot.Core;
using Narrative.Facts.Core;

namespace LevelGeneration.Journey
{
    /// <summary>
    /// Applies the biome journey to the run: on each window, resolves the covering stretch and — when
    /// it changed — switches the live theme provider (monster pools and loot rolls follow it), publishes
    /// the authored escalation tier as the world fact <c>run_escalation_tier</c> (a seam for D19;
    /// nothing consumes it yet), and notifies the appearance observer. Owns ALL theme/fact writes so
    /// there is exactly one source of truth; idempotent across repeat windows of the same stretch.
    /// Must run before the window is planned — the planner's allocators read the theme live.
    /// </summary>
    public sealed class BiomeStretchDirector
    {
        private readonly IBiomeJourney _journey;
        private readonly ICurrentThemeProvider _themeProvider;
        private readonly IFactStore _facts;
        private readonly IBiomeStretchObserver _observer;
        private readonly IGameLogger _logger;

        private int _appliedStretchIndex = -1;

        public BiomeStretchDirector(
            IBiomeJourney journey,
            ICurrentThemeProvider themeProvider,
            IFactStore facts,
            IBiomeStretchObserver observer = null,
            IGameLogger logger = null)
        {
            _journey = journey;
            _themeProvider = themeProvider;
            _facts = facts;
            _observer = observer;
            _logger = logger;
        }

        /// <summary>Ensures theme + tier fact + observers reflect the stretch covering this window.</summary>
        public void ApplyForWindow(int windowIndex)
        {
            BiomeStretch stretch = _journey.ForWindow(windowIndex);
            if (stretch.StretchIndex == _appliedStretchIndex)
            {
                return;
            }

            _appliedStretchIndex = stretch.StretchIndex;
            _themeProvider.SetTheme(stretch.Theme);
            _facts?.SetInt(WorldFacts.RunEscalationTier, stretch.EscalationTier);
            _observer?.OnBiomeStretchChanged(stretch);
            _logger?.Info(LogCategory.LevelGeneration,
                $"[BiomeStretchDirector] Stretch {stretch.StretchIndex}: {stretch.Theme} (tier {stretch.EscalationTier}) " +
                $"for windows {stretch.FirstWindow}..{stretch.EndWindowExclusive - 1}.");
        }
    }
}
