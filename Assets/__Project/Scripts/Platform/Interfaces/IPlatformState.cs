namespace Platform
{
    public interface IPlatformState
    {
        void OnEnter(IPlatform platform);
        void OnUpdate(IPlatform platform);
        void OnExit(IPlatform platform);
        bool CanTransitionTo(IPlatformState targetState);
    }
}

