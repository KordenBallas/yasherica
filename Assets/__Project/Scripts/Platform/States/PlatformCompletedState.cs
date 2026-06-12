using Loot.Application;
using Zenject;

namespace Platform
{
    public class PlatformCompletedState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<PlatformCompletedState> { }

        private readonly IQuestRewardGranter _questRewardGranter;

        public PlatformCompletedState(IQuestRewardGranter questRewardGranter)
        {
            _questRewardGranter = questRewardGranter;
        }

        public override void OnEnter(IPlatform platform)
        {
            // Platform content completed (enemy defeated, quest done, etc.)
            // Quest rewards are granted here so both completion routes
            // (dialogue ended, combat won) are covered by one hook.
            _questRewardGranter.GrantFor(platform);
        }
    }
}
