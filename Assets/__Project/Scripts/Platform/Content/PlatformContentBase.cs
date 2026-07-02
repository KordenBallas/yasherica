using Core.Logging;

namespace Platform
{
    public abstract class PlatformContentBase : IPlatformContent
    {
        public abstract ContentType Type { get; }

        /// <summary>Set by the owning <see cref="Platform"/> on add, so content can log per system.</summary>
        public IGameLogger Logger { get; set; }

        public virtual void Initialize(IPlatform platform) { }
        public virtual void OnPlatformEntered(IPlatform platform) { }
        public virtual void OnPlatformExited(IPlatform platform) { }
    }
}

