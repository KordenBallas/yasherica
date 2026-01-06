namespace Platform
{
    public class QuestContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Quest;
        
        public int QuestId { get; set; }
        
        public override void Initialize(IPlatform platform)
        {
            // Quest-specific initialization
            // Could load quest data, setup triggers, etc.
        }
        
        public override void OnPlatformEntered(IPlatform platform)
        {
            // Trigger quest event
        }
    }
}

