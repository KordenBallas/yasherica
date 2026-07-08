using Core.Logging;
using Core.Persistence;
using Heat.Core;
using Narrative.Facts.Core;

namespace Heat.Integration
{
    /// <summary>
    /// Writes the Heat high-water mark (heat-ascension FR4): when a hot run survives to a savepoint
    /// at or past the clear-window floor, the pact's total Heat becomes the new record if hotter.
    /// "Cleared" is this window-floor criterion for now — the run has no completion event until the
    /// Track Z apex exists (Known limitation). The fact is Meta-horizon, so the savepoint's own meta
    /// flush persists it (the observer runs before the flush); a later death never un-clears it.
    /// </summary>
    public sealed class HeatHighWaterRecorder : ISavepointObserver
    {
        private readonly IFactStore _facts;
        private readonly HeatRules _rules;
        private readonly HeatSettings _settings;
        private readonly IGameLogger _logger;

        public HeatHighWaterRecorder(
            IFactStore facts, HeatRules rules, HeatSettings settings, IGameLogger logger = null)
        {
            _facts = facts;
            _rules = rules ?? HeatRules.Neutral;
            _settings = settings ?? HeatSettings.Defaults;
            _logger = logger;
        }

        public void OnSavepointCaptured(RunSaveSnapshot snapshot)
        {
            if (_facts == null || snapshot?.World == null || _rules.TotalHeat <= 0)
            {
                return;
            }

            if (snapshot.World.WindowIndex < _settings.ClearWindowFloor)
            {
                return;
            }

            if (_rules.TotalHeat <= _facts.GetInt(WorldFacts.HeatHighWater))
            {
                return;
            }

            _facts.SetInt(WorldFacts.HeatHighWater, _rules.TotalHeat);
            _logger?.Info(LogCategory.Persistence,
                $"[HeatHighWaterRecorder] New hottest clear recorded: Heat {_rules.TotalHeat} " +
                $"(window {snapshot.World.WindowIndex}).");
        }
    }
}
