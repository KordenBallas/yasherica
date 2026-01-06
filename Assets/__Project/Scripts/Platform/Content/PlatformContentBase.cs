namespace Platform
{
    public abstract class PlatformContentBase : IPlatformContent
    {
        public abstract ContentType Type { get; }
        
        public virtual void Initialize(IPlatform platform) { }
        public virtual void OnPlatformEntered(IPlatform platform) { }
        public virtual void OnPlatformExited(IPlatform platform) { }
    }
}

