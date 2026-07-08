using Combat.Core;
using Combat.Execution;
using Core.Logging;

namespace Combat.Player.AI
{
    /// <summary>
    /// Simulation-based enemy decision maker (P2-4): enumerates real candidates
    /// (ability × facing, move destinations with next-round lookahead, end turn), scores
    /// them by actually-affected units, and filters the pick through the configurable
    /// decision-quality dials. Deterministic for a given seed and state.
    /// </summary>
    public sealed class SimulationTacticalAI : IAIDecisionMaker
    {
        private readonly AITuning _tuning;
        private readonly AICandidateEnumerator _enumerator;
        private readonly AIActionScorer _scorer;
        private readonly AIDecisionQualityFilter _filter;
        private readonly IGameLogger _logger;

        public SimulationTacticalAI(
            AITuning tuning,
            IAbilityOutcomeCalculator outcomes,
            IHostilityPolicy hostility,
            int seed,
            IGameLogger logger = null)
        {
            _tuning = tuning;
            _enumerator = new AICandidateEnumerator();
            _scorer = new AIActionScorer(outcomes, hostility);
            _filter = new AIDecisionQualityFilter(new System.Random(seed));
            _logger = logger;
        }

        public IAction DecideAction(ICombatState gameState, IUnit unit)
        {
            var candidates = _enumerator.Enumerate(gameState, unit, _tuning);
            var scored = _scorer.ScoreAll(gameState, unit, candidates, _tuning);
            var chosen = _filter.Pick(scored, _tuning);
            _logger?.Info(LogCategory.Combat,
                $"[SimulationTacticalAI] Unit {unit.Id}: {chosen.Kind} out of {candidates.Count} candidates.");
            return chosen.Action;
        }
    }
}
