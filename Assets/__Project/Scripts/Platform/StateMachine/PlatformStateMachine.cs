namespace Platform
{
    public class PlatformStateMachine
    {
        private IPlatformState currentState;
        private IPlatform owner;
        
        public void Initialize(IPlatform platform, IPlatformState initialState)
        {
            owner = platform;
            ChangeState(initialState);
        }
        
        public void ChangeState(IPlatformState newState)
        {
            if (currentState != null && !currentState.CanTransitionTo(newState))
                return;
                
            currentState?.OnExit(owner);
            currentState = newState;
            currentState?.OnEnter(owner);
        }
        
        public void Update()
        {
            currentState?.OnUpdate(owner);
        }
        
        public IPlatformState CurrentState => currentState;
    }
}

