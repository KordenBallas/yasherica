using Platform;

namespace Loot.Application
{
    /// <summary>
    /// Grants quest rewards directly to the player inventory when a platform
    /// with a story NPC completes (dialogue ended or combat won).
    /// </summary>
    public interface IQuestRewardGranter
    {
        void GrantFor(IPlatform platform);
    }
}
