using UnityEngine;

namespace Platform
{
    public class EnemyContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Enemy;
        
        public int EnemyId { get; set; }
        
        public override void Initialize(IPlatform platform)
        {
            // Validate that enemy content is on a combat platform
            if (platform is not CombatPlatform)
            {
                Debug.LogWarning($"[EnemyContent] Enemy content should be placed on CombatPlatform, but found on {platform.GetType().Name}");
            }
        }
        
        public override void OnPlatformEntered(IPlatform platform)
        {
            // Spawn enemy or trigger combat
        }
    }
}

