namespace Platform
{
    public class QuestContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Quest;
        
        public int QuestId { get; set; }
        
        public override void OnPlatformEntered(IPlatform platform)
        {
            // Trigger quest event
        }
    }
}

