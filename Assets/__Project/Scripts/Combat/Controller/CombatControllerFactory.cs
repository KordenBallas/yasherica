using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.Integration;
using Combat.TurnManagement;
using Core.Logging;
using Zenject;

namespace Combat.Controller
{
    /// <summary>
    /// Factory for creating ICombatController instances.
    /// Resolves all dependencies through Zenject and creates configured controller.
    /// </summary>
    public class CombatControllerFactory : IFactory<ICombatController>
    {
        private readonly IActionValidator _actionValidator;
        private readonly IActionExecutor _actionExecutor;
        private readonly ITurnManager _turnManager;
        private readonly StatusEffectTriggerProcessor _triggerProcessor;
        private readonly EnemyIntentPlanner _intentPlanner;
        private readonly EnemyIntentResolver _intentResolver;
        private readonly BattlefieldFactory _battlefieldFactory;
        private readonly HexDirectionConfig _hexConfig;
        private readonly IGameLogger _logger;
        private readonly ICombatOutcomeRelay _outcomeRelay;

        [Inject]
        public CombatControllerFactory(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            StatusEffectTriggerProcessor triggerProcessor,
            EnemyIntentPlanner intentPlanner,
            EnemyIntentResolver intentResolver,
            BattlefieldFactory battlefieldFactory,
            HexDirectionConfig hexConfig,
            IGameLogger logger,
            [InjectOptional] ICombatOutcomeRelay outcomeRelay = null)
        {
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnManager = turnManager;
            _triggerProcessor = triggerProcessor;
            _intentPlanner = intentPlanner;
            _intentResolver = intentResolver;
            _battlefieldFactory = battlefieldFactory;
            _hexConfig = hexConfig;
            _logger = logger;
            _outcomeRelay = outcomeRelay;
        }

        public ICombatController Create()
        {
            var controller = new CombatController(
                _actionValidator,
                _actionExecutor,
                _turnManager,
                _triggerProcessor,
                _intentPlanner,
                _intentResolver,
                _battlefieldFactory,
                _hexConfig,
                _logger);

            if (_outcomeRelay != null)
            {
                // Controllers are per-fight; the relay is how scene-scoped listeners (the P2-2
                // death hook) hear every fight's outcome.
                controller.OnGameEnded += (winner, phase) => _outcomeRelay.Notify(phase);
            }

            return controller;
        }
    }
}

