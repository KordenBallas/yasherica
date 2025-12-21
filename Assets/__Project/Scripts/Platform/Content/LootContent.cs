namespace Platform
{
    public class LootContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Loot;
        
        public int LootId { get; set; }
        
        public override void OnPlatformEntered(IPlatform platform)
        {
            // Spawn loot or chest
        }
    }
}

