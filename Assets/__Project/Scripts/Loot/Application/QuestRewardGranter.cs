using Core.Logging;
using Inventory.Core;
using Narrative.Quests.Core;
using Platform;

namespace Loot.Application
{
    /// <summary>
    /// Grants item rewards for any quest that completed on the platform that just finished. Scans the
    /// run-scoped <see cref="ILiveQuestRegistry"/> for a quest that reached
    /// <see cref="QuestState.Completed"/> and has not yet been paid out; each <see cref="QuestRewardCore"/>
    /// is added to the player inventory. Reading the registry (not a single dialogue's active quest) makes
    /// the payout robust to cross-dialogue continuity: a quest offered on platform A and completed on a
    /// later platform B is granted at B regardless of which dialogue is current. Driven from the single
    /// completion hook (<c>PlatformCompletedState.OnEnter</c>) so both completion routes (dialogue ended,
    /// combat won) are covered. Item rewards only.
    /// </summary>
    public class QuestRewardGranter : IQuestRewardGranter
    {
        private readonly ILiveQuestRegistry _quests;
        private readonly IInventoryModel _inventory;
        private readonly IGameLogger _logger;

        public QuestRewardGranter(ILiveQuestRegistry quests, IInventoryModel inventory, IGameLogger logger)
        {
            _quests = quests;
            _inventory = inventory;
            _logger = logger;
        }

        public void GrantFor(IPlatform platform)
        {
            var live = _quests?.LiveQuests;
            if (live == null)
            {
                return;
            }

            for (int q = 0; q < live.Count; q++)
            {
                var quest = live[q];
                if (quest.State != QuestState.Completed || quest.RewardsGranted)
                {
                    continue; // not freshly completed (loot platforms use the roll path)
                }

                quest.MarkRewardsGranted();
                var rewards = quest.Data.Rewards;
                for (int i = 0; i < rewards.Count; i++)
                {
                    var reward = rewards[i];
                    if (string.IsNullOrEmpty(reward.ArtifactId))
                    {
                        continue;
                    }

                    for (int n = 0; n < reward.Count; n++)
                    {
                        _inventory.Add(reward.ArtifactId);
                    }
                }

                _logger?.Info(LogCategory.Loot,$"[QuestRewardGranter] Granted {rewards.Count} reward stack(s) for quest '{quest.Data.QuestId}'.");
            }
        }
    }
}
