namespace Platform
{
    public class EnemyContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Enemy;
        
        public int EnemyId { get; set; }
        
        public override void OnPlatformEntered(IPlatform platform)
        {
            // Spawn enemy or trigger combat
        }
    }
}

