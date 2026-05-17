using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Generation
{
    /// <summary>
    /// Binds NPCs, locations, and rewards to story templates.
    /// Fills parameter slots with concrete runtime values.
    /// Pure C# class - no Unity dependencies except logging and Random.
    /// </summary>
    public class ParameterBinder : IParameterBinder
    {
        private readonly IReadOnlyList<RewardDefinition> _rewardDefinitions;

        /// <summary>
        /// Creates a new parameter binder.
        /// </summary>
        public ParameterBinder(IReadOnlyList<RewardDefinition> rewardDefinitions = null)
        {
            _rewardDefinitions = rewardDefinitions ?? Array.Empty<RewardDefinition>();
        }

        public BoundStory BindParameters(
            StoryTemplateSelection selection,
            INarrativeContext context,
            INpcPool npcPool)
        {
            if (selection?.Template == null || context == null)
            {
                Debug.LogWarning("[ParameterBinder] Invalid selection or context");
                return null;
            }

            var boundParams = new Dictionary<string, BoundParameter>();
            var boundNpcs = new List<NpcInstance>();
            var boundRewards = new List<RewardInstance>();
            string boundLocationId = null;

            // Bind each parameter slot
            foreach (var slot in selection.RequiredParameters)
            {
                var boundParam = BindParameter(slot, context, npcPool);

                if (boundParam == null && slot.IsRequired)
                {
                    Debug.LogWarning($"[ParameterBinder] Failed to bind required parameter '{slot.ParameterId}'");
                    // Release any NPCs we've already reserved
                    foreach (var npc in boundNpcs)
                    {
                        npcPool.ReleaseNpc(npc.InstanceId);
                    }
                    return null;
                }

                if (boundParam != null)
                {
                    boundParams[slot.ParameterId] = boundParam;

                    // Track bound NPCs
                    if (slot.ParameterType == ParameterType.Npc && boundParam.Value is NpcInstance npcInstance)
                    {
                        boundNpcs.Add(npcInstance);
                    }

                    // Track bound location
                    if (slot.ParameterType == ParameterType.Location && boundParam.Value is string locationId)
                    {
                        boundLocationId = locationId;
                    }
                }
            }

            // Bind rewards from template reward slots
            foreach (var rewardSlot in selection.Template.RewardSlots)
            {
                var reward = BindReward(rewardSlot, context);
                if (reward != null)
                {
                    boundRewards.Add(reward);
                }
            }

            Debug.Log($"[ParameterBinder] Bound {boundParams.Count} parameters, {boundNpcs.Count} NPCs, {boundRewards.Count} rewards");

            return new BoundStory(
                selection.Template,
                boundParams,
                boundNpcs,
                boundLocationId,
                boundRewards);
        }

        public BoundParameter BindParameter(
            TemplateParameterSlot slot,
            INarrativeContext context,
            INpcPool npcPool)
        {
            if (slot == null)
                return null;

            switch (slot.ParameterType)
            {
                case ParameterType.Npc:
                    return BindNpcParameter(slot, context, npcPool);

                case ParameterType.Location:
                    return BindLocationParameter(slot, context);

                case ParameterType.Reward:
                    return BindRewardParameter(slot, context);

                case ParameterType.ItemName:
                    return BindItemNameParameter(slot, context);

                case ParameterType.Quantity:
                    return BindQuantityParameter(slot, context);

                case ParameterType.Custom:
                    return BindCustomParameter(slot, context);

                default:
                    Debug.LogWarning($"[ParameterBinder] Unknown parameter type: {slot.ParameterType}");
                    return null;
            }
        }

        public bool CanBindAllParameters(
            StoryTemplateSelection selection,
            INarrativeContext context,
            INpcPool npcPool)
        {
            if (selection?.RequiredParameters == null)
                return true;

            int requiredNpcs = 0;
            foreach (var slot in selection.RequiredParameters)
            {
                if (slot.ParameterType == ParameterType.Npc && slot.IsRequired)
                    requiredNpcs++;
            }

            // Check if we have enough NPCs
            int availableNpcs = npcPool?.AvailableNpcs?.Count ?? 0;
            if (availableNpcs < requiredNpcs)
            {
                Debug.Log($"[ParameterBinder] Not enough NPCs: need {requiredNpcs}, have {availableNpcs}");
                return false;
            }

            return true;
        }

        private BoundParameter BindNpcParameter(
            TemplateParameterSlot slot,
            INarrativeContext context,
            INpcPool npcPool)
        {
            if (npcPool == null)
                return null;

            NpcInstance selectedNpc = null;

            // Check for specific NPC requirement
            if (slot.HasSpecificNpc)
            {
                selectedNpc = npcPool.GetNpc(slot.SpecificNpcId);
                if (selectedNpc == null || !selectedNpc.IsAvailable)
                {
                    Debug.LogWarning($"[ParameterBinder] Specific NPC '{slot.SpecificNpcId}' not available");
                    return null;
                }
            }
            else
            {
                // Search for matching NPC
                var criteria = new NpcSearchCriteria
                {
                    RequiredFaction = slot.EnforceFaction ? slot.RequiredFaction : null,
                    RequiredRole = slot.RequiredRole != NpcRole.None ? slot.RequiredRole : null,
                    RequiredTraits = new List<string>(slot.RequiredTraits),
                    MaxResults = 5
                };

                var matches = npcPool.FindMatchingNpcs(criteria);
                if (matches.Count > 0)
                {
                    // Select randomly from top matches for variety
                    int index = UnityEngine.Random.Range(0, Math.Min(3, matches.Count));
                    selectedNpc = matches[index];
                }
            }

            if (selectedNpc == null)
            {
                Debug.LogWarning($"[ParameterBinder] No matching NPC found for slot '{slot.ParameterId}'");
                return null;
            }

            // Reserve the NPC
            NpcRole role = slot.RequiredRole != NpcRole.None ? slot.RequiredRole : NpcRole.QuestGiver;
            if (!npcPool.ReserveNpc(selectedNpc.InstanceId, role, context.CurrentAreaId))
            {
                return null;
            }

            return new BoundParameter(
                slot,
                selectedNpc,
                selectedNpc.DisplayName,
                selectedNpc.DisplayName);
        }

        private BoundParameter BindLocationParameter(TemplateParameterSlot slot, INarrativeContext context)
        {
            // For now, use current area as location
            string locationId = context.CurrentAreaId;
            string displayName = GetLocationDisplayName(locationId, slot.LocationType);

            return new BoundParameter(slot, locationId, displayName, displayName);
        }

        private BoundParameter BindRewardParameter(TemplateParameterSlot slot, INarrativeContext context)
        {
            var reward = FindMatchingReward(slot.RewardType, context.CurrentChapterNumber);
            if (reward == null)
            {
                // Create a default currency reward
                int value = UnityEngine.Random.Range(slot.RewardValueRange.x, slot.RewardValueRange.y + 1);
                return new BoundParameter(slot, value, $"{value} gold", value.ToString());
            }

            var instance = new RewardInstance(reward, context.CurrentChapterNumber);
            return new BoundParameter(slot, instance, instance.GetDisplayString(), instance.CalculatedValue.ToString());
        }

        private BoundParameter BindItemNameParameter(TemplateParameterSlot slot, INarrativeContext context)
        {
            // Placeholder - would integrate with item system
            string itemName = GenerateItemName(slot, context);
            return new BoundParameter(slot, itemName, itemName, itemName);
        }

        private BoundParameter BindQuantityParameter(TemplateParameterSlot slot, INarrativeContext context)
        {
            int quantity = UnityEngine.Random.Range(slot.RewardValueRange.x, slot.RewardValueRange.y + 1);
            return new BoundParameter(slot, quantity, quantity.ToString(), quantity.ToString());
        }

        private BoundParameter BindCustomParameter(TemplateParameterSlot slot, INarrativeContext context)
        {
            // Custom parameters would be handled based on specific needs
            return new BoundParameter(slot, slot.DisplayName, slot.DisplayName, slot.DisplayName);
        }

        private RewardInstance BindReward(TemplateRewardSlot rewardSlot, INarrativeContext context)
        {
            if (rewardSlot?.RewardDefinition == null)
                return null;

            // Check probability
            if (UnityEngine.Random.value > rewardSlot.Probability)
                return null;

            return new RewardInstance(
                rewardSlot.RewardDefinition,
                context.CurrentChapterNumber,
                rewardSlot.ValueMultiplier,
                rewardSlot.Condition);
        }

        private RewardDefinition FindMatchingReward(RewardType type, int chapterNumber)
        {
            var candidates = new List<RewardDefinition>();
            float totalWeight = 0f;

            foreach (var reward in _rewardDefinitions)
            {
                if (reward.RewardType == type && reward.MinimumChapter <= chapterNumber)
                {
                    candidates.Add(reward);
                    totalWeight += reward.DropWeight;
                }
            }

            if (candidates.Count == 0)
                return null;

            // Weighted random selection
            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var candidate in candidates)
            {
                cumulative += candidate.DropWeight;
                if (roll <= cumulative)
                    return candidate;
            }

            return candidates[candidates.Count - 1];
        }

        private string GetLocationDisplayName(string locationId, LocationType locationType)
        {
            // Would integrate with location system
            switch (locationType)
            {
                case LocationType.Town:
                    return "the village";
                case LocationType.Wilderness:
                    return "the forest path";
                case LocationType.Dungeon:
                    return "the ancient ruins";
                case LocationType.Shop:
                    return "the market";
                case LocationType.Landmark:
                    return "the old tower";
                default:
                    return "this area";
            }
        }

        private string GenerateItemName(TemplateParameterSlot slot, INarrativeContext context)
        {
            // Placeholder item name generation
            string[] items = { "ancient artifact", "mysterious scroll", "rare gem", "enchanted blade", "healing herb" };
            return items[UnityEngine.Random.Range(0, items.Length)];
        }
    }
}
