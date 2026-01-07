using Combat.Controller;
using Core.Camera;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Combat platform that creates its own CombatController during initialization.
    /// All combat logic is encapsulated in CombatActiveState.
    /// </summary>
    public class CombatPlatform : Platform
    {
        private readonly IFactory<ICombatController> _controllerFactory;
        private readonly ICameraService _cameraService;
        private ICombatController _controller;
        private IPlatformState _combatActiveState;
        
        /// <summary>
        /// The battlefield instance for this combat platform.
        /// </summary>
        public Combat.Battlefield.IBattlefield Battlefield => _controller?.Battlefield;
        
        public CombatPlatform(int id, IFactory<ICombatController> controllerFactory, ICameraService cameraService) : base(id)
        {
            _controllerFactory = controllerFactory;
            _cameraService = cameraService;
        }
        
        public override void Initialize(IPlatformVisual visual)
        {
            base.Initialize(visual); // Initialize content first
            
            // Create combat controller
            _controller = _controllerFactory.Create();
            
            // Create combat active state that handles battlefield initialization and camera management
            _combatActiveState = new CombatActiveState(_controller, _cameraService);
        }
        
        protected override void InitializeStateMachine()
        {
            StateMachine.Initialize(this, new PlatformIdleState());
        }
        
    public override void Enter()
    {
        // Transition to Combat active state
        // This state will initialize battlefield and switch to combat camera
        StateMachine.ChangeState(_combatActiveState);
    }
        
        public class Factory : PlaceholderFactory<CombatPlatform>
        {
        }
    }
}
