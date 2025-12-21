using Platform;

namespace Core.Events
{
    public class PlatformEvents
    {
        public static System.Action<IPlatform> OnPlatformEntered;
        public static System.Action<IPlatform> OnPlatformExited;
        public static System.Action<IPlatform, IPlatform> OnPlatformChanged; // old, new
    }
}

