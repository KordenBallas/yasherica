using Combat.Execution;
using Core.Logging;

namespace Combat.Player.AI
{
    /// <summary>
    /// The one place tactical decision makers are built: composes the enemy's behavior
    /// profile with the globally bound difficulty and hostility policy, so PvE enemies and
    /// Arena offline dummies get the same brain with mode-appropriate alliances.
    /// </summary>
    public sealed class AIDecisionMakerFactory
    {
        private readonly IAbilityOutcomeCalculator _outcomeCalculator;
        private readonly IHostilityPolicy _hostilityPolicy;
        private readonly IAIDifficultySource _difficultySource;
        private readonly IGameLogger _logger;

        public AIDecisionMakerFactory(
            IAbilityOutcomeCalculator outcomeCalculator,
            IHostilityPolicy hostilityPolicy,
            IAIDifficultySource difficultySource,
            IGameLogger logger)
        {
            _outcomeCalculator = outcomeCalculator;
            _hostilityPolicy = hostilityPolicy;
            _difficultySource = difficultySource;
            _logger = logger;
        }

        public IAIDecisionMaker Create(AIBehaviorProfile profile, int seed)
        {
            var tuning = AITuning.Compose(
                profile ?? AIBehaviorProfile.Default, _difficultySource.Current);
            return new SimulationTacticalAI(
                tuning, _outcomeCalculator, _hostilityPolicy, seed, _logger);
        }
    }
}
