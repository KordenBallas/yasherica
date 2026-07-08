using Core.Logging;
using Loot.Core;
using Narrative.Facts.Core;

namespace LevelGeneration.Journey
{
    /// <summary>
    /// Applies the biome journey to the run: on each window, resolves the covering stretch and — when
    /// it changed — switches the live theme provider (monster pools and loot rolls follow it), publishes
    /// the EFFECTIVE escalation tier (the authored stretch tier plus any Heat lift, Track Y) as the
    /// world fact <c>run_escalation_tier</c> — consumed by the window planner's story tier-bands and
    /// monster pool draws (D19) — and notifies the appearance observer. Owns ALL theme/fact writes so
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
        private readonly JourneyRuleModifiers _rules;

        private int _appliedStretchIndex = -1;

        public BiomeStretchDirector(
            IBiomeJourney journey,
            ICurrentThemeProvider themeProvider,
            IFactStore facts,
            IBiomeStretchObserver observer = null,
            IGameLogger logger = null,
            JourneyRuleModifiers rules = null)
        {
            _journey = journey;
            _themeProvider = themeProvider;
            _facts = facts;
            _observer = observer;
            _logger = logger;
            _rules = rules ?? JourneyRuleModifiers.Neutral;
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
            int effectiveTier = stretch.EscalationTier + _rules.EscalationTierLift;
            _facts?.SetInt(WorldFacts.RunEscalationTier, effectiveTier);
            _observer?.OnBiomeStretchChanged(stretch);
            _logger?.Info(LogCategory.LevelGeneration,
                $"[BiomeStretchDirector] Stretch {stretch.StretchIndex}: {stretch.Theme} (tier {effectiveTier}" +
                $"{(_rules.EscalationTierLift > 0 ? $" = {stretch.EscalationTier} + heat {_rules.EscalationTierLift}" : string.Empty)}) " +
                $"for windows {stretch.FirstWindow}..{stretch.EndWindowExclusive - 1}.");
        }
    }
}
