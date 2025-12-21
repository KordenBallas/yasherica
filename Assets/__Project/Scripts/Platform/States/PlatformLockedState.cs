namespace Platform
{
    public class PlatformLockedState : PlatformStateBase
    {
        public override void OnEnter(IPlatform platform)
        {
            // Platform is locked (not accessible yet)
        }
        
        public override bool CanTransitionTo(IPlatformState targetState)
        {
            // Can only transition from Locked if prerequisites are met
            return targetState is PlatformIdleState;
        }
    }
}

