using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Inventory.Data;
using Loot.Core;
using Narrative.Data.Definitions;
using Narrative.Generation;
using Platform;

namespace Loot.Application
{
    /// <summary>
    /// Bridges the narrative reward system to the artifact inventory.
    /// Delivers (1) the fixed/bonus rewards resolved onto NpcAssignment.Rewards at
    /// narrative generation (item rewards reference ArtifactDefinition.Id through
    /// RewardDefinition.ItemId) and (2) a procedural component rolled from the
    /// biome quest table, biased by story/NPC tags. Direct grant - no pickup entity.
    /// </summary>
    public class QuestRewardGranter : IQuestRewardGranter
    {
        private readonly ILootRollService _lootRollService;
        private readonly ICurrentThemeProvider _themeProvider;
        private readonly IInventoryModel _inventory;
        private readonly IArtifactCatalog _artifactCatalog;
        private readonly IGameLogger _logger;
        private readonly HashSet<int> _grantedPlatformIds = new HashSet<int>();

        public QuestRewardGranter(
            ILootRollService lootRollService,
            ICurrentThemeProvider themeProvider,
            IInventoryModel inventory,
            IArtifactCatalog artifactCatalog,
            IGameLogger logger)
        {
            _lootRollService = lootRollService;
            _themeProvider = themeProvider;
            _inventory = inventory;
            _artifactCatalog = artifactCatalog;
            _logger = logger;
        }

        public void GrantFor(IPlatform platform)
        {
            // Idempotence: a platform can re-enter the completed state, but its
            // quest rewards are granted once.
            if (platform == null || !_grantedPlatformIds.Add(platform.Id))
            {
                return;
            }

            foreach (var content in platform.Contents)
            {
                if (content is NpcContent npcContent && npcContent.Assignment?.HasStory == true)
                {
                    GrantForAssignment(npcContent.Assignment);
                }
            }
        }

        /// <summary>
        /// Grants rewards for one story assignment. Public for testability;
        /// production code goes through GrantFor.
        /// </summary>
        public void GrantForAssignment(NpcAssignment assignment)
        {
            GrantFixedRewards(assignment);
            GrantProceduralRewards(assignment);
        }

        private void GrantFixedRewards(NpcAssignment assignment)
        {
            foreach (var reward in assignment.Rewards)
            {
                var definition = reward.Definition;
                if (definition == null)
                {
                    continue;
                }

                if (definition.RewardType != RewardType.Item
                    && definition.RewardType != RewardType.Equipment)
                {
                    // Only item-like rewards map to artifacts; other types have
                    // no receiving system yet.
                    _logger.Info(
                        $"[QuestRewardGranter] Skipping non-item reward '{definition.RewardId}' " +
                        $"({definition.RewardType})");
                    continue;
                }

                if (!definition.HasItem || !_artifactCatalog.TryGet(definition.ItemId, out _))
                {
                    _logger.Warning(
                        $"[QuestRewardGranter] Reward '{definition.RewardId}' references unknown " +
                        $"artifact id '{definition.ItemId}' - skipped");
                    continue;
                }

                AddToInventory(definition.ItemId, reward.Quantity, "fixed");
            }
        }

        private void GrantProceduralRewards(NpcAssignment assignment)
        {
            var story = assignment.Story;
            var tags = BuildContextTags(assignment);
            var context = new LootRollContext(
                _themeProvider.CurrentTheme,
                $"quest:{story.StoryId}:{assignment.Npc.NpcId}",
                tags);

            var results = _lootRollService.RollQuestRewards(context);
            foreach (var result in results)
            {
                AddToInventory(result.ArtifactId, result.Quantity, $"procedural [{context.ContextKey}]");
            }
        }

        private static IReadOnlyList<string> BuildContextTags(NpcAssignment assignment)
        {
            var tags = new List<string>();
            if (assignment.Story.Tags != null)
            {
                tags.AddRange(assignment.Story.Tags);
            }

            if (assignment.Npc.Tags != null)
            {
                tags.AddRange(assignment.Npc.Tags);
            }

            return tags;
        }

        private void AddToInventory(string artifactId, int quantity, string source)
        {
            for (int i = 0; i < quantity; i++)
            {
                _inventory.Add(artifactId);
            }

            _logger.Info($"[QuestRewardGranter] Granted {quantity}x '{artifactId}' ({source})");
        }
    }
}
