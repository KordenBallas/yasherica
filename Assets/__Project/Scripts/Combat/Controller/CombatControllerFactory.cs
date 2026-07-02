using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
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
        private readonly IDamageSystem _damageSystem;
        private readonly StatusEffectTriggerProcessor _triggerProcessor;
        private readonly BattlefieldFactory _battlefieldFactory;
        private readonly HexDirectionConfig _hexConfig;
        private readonly IGameLogger _logger;

        [Inject]
        public CombatControllerFactory(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            IDamageSystem damageSystem,
            StatusEffectTriggerProcessor triggerProcessor,
            BattlefieldFactory battlefieldFactory,
            HexDirectionConfig hexConfig,
            IGameLogger logger)
        {
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnManager = turnManager;
            _damageSystem = damageSystem;
            _triggerProcessor = triggerProcessor;
            _battlefieldFactory = battlefieldFactory;
            _hexConfig = hexConfig;
            _logger = logger;
        }

        public ICombatController Create()
        {
            return new CombatController(
                _actionValidator,
                _actionExecutor,
                _turnManager,
                _damageSystem,
                _triggerProcessor,
                _battlefieldFactory,
                _hexConfig,
                _logger);
        }
    }
}

