using Core.Logging;
using Platform;

namespace Loot.Application
{
    /// <summary>
    /// Grants platform-completion rewards. The legacy story-reward path (resolved onto an
    /// <c>NpcAssignment</c> at narrative-generation time) was removed with the legacy narrative engine;
    /// the new data-driven engine carries no item-reward sink yet, so this is currently a no-op kept as
    /// the completion hook (`PlatformCompletedState`). Re-homing rewards (quest-carried item rewards) is a
    /// ROADMAP item.
    /// </summary>
    public class QuestRewardGranter : IQuestRewardGranter
    {
        private readonly IGameLogger _logger;

        public QuestRewardGranter(IGameLogger logger)
        {
            _logger = logger;
        }

        public void GrantFor(IPlatform platform)
        {
            // No reward sink for the new engine yet; intentionally a no-op (see ROADMAP "quest-carried
            // item rewards"). Loot platforms grant via the loot roll path, not here.
        }
    }
}
