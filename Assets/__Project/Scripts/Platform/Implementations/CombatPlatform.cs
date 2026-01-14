using Character;
using Combat.Controller;
using Combat.Core;
using Combat.Data;
using Combat.Integration;
using Combat.Input;
using Core.Camera;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Combat platform that creates its own CombatController during initialization.
    /// All combat logic is encapsulated in CombatActiveState.
    /// Now includes enemy combat integration support.
    /// </summary>
    public class CombatPlatform : Platform
    {
        private readonly IFactory<ICombatController> _controllerFactory;
        private readonly ICameraService _cameraService;
        private readonly CharacterCombatInitializer _characterInitializer;
        private readonly IInputController _inputController;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly ICharacterRegistry _characterRegistry;
        private readonly EnemyCombatIntegrator _enemyIntegrator;
        private readonly IEnemyDataProvider _enemyDataProvider;
        private readonly Combat.Player.AITurnController _aiTurnController;
        private ICombatController _controller;
        private IPlatformState _combatActiveState;

        /// <summary>
        /// The battlefield instance for this combat platform.
        /// </summary>
        public Combat.Battlefield.IBattlefield Battlefield => _controller?.Battlefield;

        public CombatPlatform(
            int id,
            IFactory<ICombatController> controllerFactory,
            ICameraService cameraService,
            CharacterCombatInitializer characterInitializer,
            IInputController inputController,
            IPlayerRegistry playerRegistry,
            ICharacterRegistry characterRegistry,
            EnemyCombatIntegrator enemyIntegrator,
            IEnemyDataProvider enemyDataProvider,
            Combat.Player.AITurnController aiTurnController) : base(id)
        {
            _controllerFactory = controllerFactory;
            _cameraService = cameraService;
            _characterInitializer = characterInitializer;
            _inputController = inputController;
            _playerRegistry = playerRegistry;
            _characterRegistry = characterRegistry;
            _enemyIntegrator = enemyIntegrator;
            _enemyDataProvider = enemyDataProvider;
            _aiTurnController = aiTurnController;
        }
        
        public override void Initialize(IPlatformVisual visual)
        {
            base.Initialize(visual); // Initialize content first

            // Create combat controller
            _controller = _controllerFactory.Create();

            // Subscribe to combat end event
            _controller.OnGameEnded += HandleCombatEnded;

            // Create combat active state that handles battlefield initialization and camera management
            _combatActiveState = new CombatActiveState(
                _controller,
                _cameraService,
                _characterInitializer,
                _inputController,
                _playerRegistry,
                _characterRegistry,
                _enemyIntegrator,
                _aiTurnController);  // Pass AI turn controller
        }

        protected override void InitializeStateMachine()
        {
            // Create idle state with enemy integrator dependencies
            StateMachine.Initialize(this,
                new PlatformIdleState(_enemyIntegrator, _enemyDataProvider));
        }
        
    public override void Enter()
    {
        // Transition to Combat active state
        // This state will initialize battlefield and switch to combat camera
        StateMachine.ChangeState(_combatActiveState);
    }

        private void HandleCombatEnded(IPlayer winner, CombatPhase phase)
        {
            Debug.Log($"[CombatPlatform] Combat ended - Phase: {phase}, Winner: {winner?.Name ?? "None"}");

            // Transition to completed state for both Victory and Defeat
            StateMachine.ChangeState(new PlatformCompletedState());
        }

        public void Dispose()
        {
            if (_controller != null)
            {
                _controller.OnGameEnded -= HandleCombatEnded;
            }
        }

        public class Factory : PlaceholderFactory<CombatPlatform>
        {
        }
    }
}
