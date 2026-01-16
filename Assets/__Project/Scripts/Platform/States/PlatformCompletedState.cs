using Zenject;

namespace Platform
{
    public class PlatformCompletedState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<PlatformCompletedState> { }

        public override void OnEnter(IPlatform platform)
        {
            // Platform content completed (enemy defeated, quest done, etc.)
        }
    }
}

