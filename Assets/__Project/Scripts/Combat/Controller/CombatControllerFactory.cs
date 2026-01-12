using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.TurnManagement;
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
        private readonly BattlefieldFactory _battlefieldFactory;
        private readonly CombatConfig _config;
        private readonly HexDirectionConfig _hexConfig;

        [Inject]
        public CombatControllerFactory(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            IDamageSystem damageSystem,
            BattlefieldFactory battlefieldFactory,
            CombatConfig config,
            HexDirectionConfig hexConfig)
        {
            _actionValidator = actionValidator;
            _actionExecutor = actionExecutor;
            _turnManager = turnManager;
            _damageSystem = damageSystem;
            _battlefieldFactory = battlefieldFactory;
            _config = config;
            _hexConfig = hexConfig;
        }

        public ICombatController Create()
        {
            return new CombatController(
                _actionValidator,
                _actionExecutor,
                _turnManager,
                _damageSystem,
                _battlefieldFactory,
                _config,
                _hexConfig);
        }
    }
}

