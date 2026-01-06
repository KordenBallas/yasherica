using Combat.Controller;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Combat platform that creates its own CombatController during initialization.
    /// All combat logic is encapsulated in CombatPlatformActiveState.
    /// </summary>
    public class CombatPlatform : Platform
    {
        private readonly IFactory<ICombatController> _controllerFactory;
        private ICombatController _controller;
        private IPlatformState _combatActiveState;
        
        /// <summary>
        /// The battlefield instance for this combat platform.
        /// </summary>
        public Combat.Battlefield.IBattlefield Battlefield => _controller?.Battlefield;
        
        public CombatPlatform(int id, IFactory<ICombatController> controllerFactory) : base(id)
        {
            _controllerFactory = controllerFactory;
        }
        
        public override void Initialize(IPlatformVisual visual)
        {
            base.Initialize(visual); // Initialize content first
            
            // Create combat controller
            _controller = _controllerFactory.Create();
            
            // Create combat-specific active state with controller
            _combatActiveState = new CombatPlatformActiveState(_controller);
        }
        
        protected override void InitializeStateMachine()
        {
            StateMachine.Initialize(this, new PlatformIdleState());
        }
        
        public new void Enter()
        {
            // Transition to Combat-specific active state
            // This state will handle Combat initialization
            StateMachine.ChangeState(_combatActiveState);
        }
        
        public class Factory : PlaceholderFactory<CombatPlatform>
        {
        }
    }
}
