namespace Platform
{
    public abstract class PlatformStateBase : IPlatformState
    {
        public virtual void OnEnter(IPlatform platform) { }
        public virtual void OnUpdate(IPlatform platform) { }
        public virtual void OnExit(IPlatform platform) { }
        
        public virtual bool CanTransitionTo(IPlatformState targetState)
        {
            return true;  // Override in derived classes for specific rules
        }
    }
}

