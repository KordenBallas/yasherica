namespace Platform
{
    public class PlatformActiveState : PlatformStateBase
    {
        public override void OnEnter(IPlatform platform)
        {
            // Player is on platform, content is active
            foreach (var content in platform.Contents)
            {
                content.OnPlatformEntered(platform);
            }
        }
        
        public override void OnExit(IPlatform platform)
        {
            foreach (var content in platform.Contents)
            {
                content.OnPlatformExited(platform);
            }
        }
    }
}

