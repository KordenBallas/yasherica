namespace Platform
{
    public enum ContentType
    {
        None,
        Enemy,
        Npc,
        Loot,
        Quest,
        Village,
        Crossroad
    }

    public interface IPlatformContent
    {
        ContentType Type { get; }
        void OnPlatformEntered(IPlatform platform);
        void OnPlatformExited(IPlatform platform);
    }
}

