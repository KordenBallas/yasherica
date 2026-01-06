using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Platform installer - currently empty as platform factory bindings
    /// are handled in GameInstaller to avoid circular dependencies.
    /// This installer is kept for future platform-specific bindings.
    /// </summary>
    public class PlatformInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Platform factory bindings are in GameInstaller
            // to avoid dependency resolution issues during installation.
            // 
            // Note: IPlatformFactoryRegistry is bound but not currently used
            // by AreaGenerator, which creates platforms directly.
            // This is intentional to avoid factory abstraction overhead.
        }
    }
}

