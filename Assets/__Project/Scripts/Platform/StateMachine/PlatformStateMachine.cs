using Core.Logging;
using UnityEngine;

namespace Platform
{
    public class PlatformStateMachine
    {
        private IPlatformState currentState;
        private IPlatform owner;
        private readonly IGameLogger _logger;

        public PlatformStateMachine(IGameLogger logger = null)
        {
            _logger = logger;
        }

        public void Initialize(IPlatform platform, IPlatformState initialState)
        {
            owner = platform;
            ChangeState(initialState);
        }
        
        public void ChangeState(IPlatformState newState)
        {
            if (currentState != null && !currentState.CanTransitionTo(newState))
            {
                _logger?.Warning(LogCategory.Platform, $"[PlatformStateMachine] Platform {owner?.Id}: Cannot transition from {currentState?.GetType().Name} to {newState?.GetType().Name}");
                return;
            }
            
            string oldStateName = currentState?.GetType().Name ?? "null";
            string newStateName = newState?.GetType().Name ?? "null";
            _logger?.Info(LogCategory.Platform, $"[PlatformStateMachine] Platform {owner?.Id}: {oldStateName} -> {newStateName}");
                
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

