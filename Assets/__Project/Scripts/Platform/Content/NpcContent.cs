namespace Platform
{
    public class NpcContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Npc;
        
        public int NpcId { get; set; }
        
        public override void Initialize(IPlatform platform)
        {
            // NPC-specific initialization
            // Could load NPC data, setup dialogue, etc.
        }
        
        public override void OnPlatformEntered(IPlatform platform)
        {
            // Show NPC dialogue or interaction
        }
    }
}

