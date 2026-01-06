using UnityEngine;

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
            {
                Debug.LogWarning($"[PlatformStateMachine] Platform {owner?.Id}: Cannot transition from {currentState?.GetType().Name} to {newState?.GetType().Name}");
                return;
            }
            
            string oldStateName = currentState?.GetType().Name ?? "null";
            string newStateName = newState?.GetType().Name ?? "null";
            Debug.Log($"[PlatformStateMachine] Platform {owner?.Id}: {oldStateName} -> {newStateName}");
                
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

