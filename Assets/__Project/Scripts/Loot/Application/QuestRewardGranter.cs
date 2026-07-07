using Core.Logging;
using Inventory.Core;
using Loot.Core;
using Mutation.Core;
using Narrative.Quests.Core;
using Platform;

namespace Loot.Application
{
    /// <summary>
    /// Pays out any quest that completed on the platform that just finished (P1-5). Scans the
    /// run-scoped <see cref="ILiveQuestRegistry"/> for a quest that reached
    /// <see cref="QuestState.Completed"/> and has not yet been paid; each declared reward
    /// (tier + belonging + payload kind) is ROLLED into a concrete item by the
    /// <see cref="IQuestRewardRoller"/> and routed by kind — an artifact into the cauldron inventory,
    /// a Part-Blank onto the blank rack. Reading the registry (not a single dialogue's active quest)
    /// keeps the payout robust to cross-dialogue continuity. Driven from the single completion hook
    /// (<c>PlatformCompletedState.OnEnter</c>) so both completion routes (dialogue ended, combat won)
    /// are covered. Item rewards only; the quest's fact effects are its own channel.
    /// </summary>
    public class QuestRewardGranter : IQuestRewardGranter
    {
        private readonly ILiveQuestRegistry _quests;
        private readonly IInventoryModel _inventory;
        private readonly IBlankRack _blankRack;
        private readonly IQuestRewardRoller _roller;
        private readonly IGameLogger _logger;

        public QuestRewardGranter(ILiveQuestRegistry quests, IInventoryModel inventory,
            IBlankRack blankRack, IQuestRewardRoller roller, IGameLogger logger)
        {
            _quests = quests;
            _inventory = inventory;
            _blankRack = blankRack;
            _roller = roller;
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
                GrantRewards(quest);
            }
        }

        private void GrantRewards(QuestInstance quest)
        {
            var rewards = quest.Data.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                // The context key is stable per (quest, reward index): deterministic under the run
                // seed, independent of when along the run the quest completes.
                var contextKey = $"{quest.Data.QuestId}:{i}";
                if (!_roller.TryRoll(rewards[i], contextKey, out var rolled))
                {
                    _logger?.Warning(LogCategory.Loot,
                        $"[QuestRewardGranter] Quest '{quest.Data.QuestId}' reward {i} " +
                        $"(kind {rewards[i].PayloadKind}, belonging '{rewards[i].BelongingId}') has an " +
                        "empty roll pool - nothing granted. Check the authored belonging id.");
                    continue;
                }

                Deliver(quest, rolled);
            }
        }

        private void Deliver(QuestInstance quest, QuestRewardRollResult rolled)
        {
            switch (rolled.PayloadKind)
            {
                case QuestRewardPayloadKind.Artifact:
                    _inventory.Add(rolled.DefinitionId);
                    _logger?.Info(LogCategory.Loot,
                        $"[QuestRewardGranter] Quest '{quest.Data.QuestId}' rolled artifact " +
                        $"'{rolled.DefinitionId}'.");
                    break;

                case QuestRewardPayloadKind.PartBlank:
                    if (_blankRack.TryAdd(rolled.DefinitionId, out _))
                    {
                        _logger?.Info(LogCategory.Loot,
                            $"[QuestRewardGranter] Quest '{quest.Data.QuestId}' rolled Part-Blank " +
                            $"'{rolled.DefinitionId}'.");
                    }
                    else
                    {
                        // The rack cap IS the incubation tension; a full rack forfeits the blank
                        // rather than bypassing the cap (logged so the loss is visible).
                        _logger?.Warning(LogCategory.Loot,
                            $"[QuestRewardGranter] Quest '{quest.Data.QuestId}' rolled Part-Blank " +
                            $"'{rolled.DefinitionId}' but the rack is full - the blank is forfeit.");
                    }

                    break;
            }
        }
    }
}
