using System;
using System.Collections.Generic;
using Narrative.Quests.Core;

namespace Loot.Core
{
    /// <summary>
    /// Default <see cref="IQuestRewardRoller"/> (P1-5). The declared belonging is a hard family
    /// filter (the card's colour can never promise a family the roll won't deliver); the declared
    /// tier is a bias with graceful degrade — exact tier first, then the nearest authored tier, so a
    /// sparse pool still pays out at the promised family. Every roll derives its own Random from
    /// (runSeed, contextKey), mirroring <see cref="LootRollService"/>, so outcomes are reproducible
    /// within a run and independent of roll order.
    /// </summary>
    public sealed class QuestRewardRoller : IQuestRewardRoller
    {
        private const string ContextKeyPrefix = "quest-reward:";

        private readonly QuestRewardPools _pools;
        private readonly IRunSeedProvider _seedProvider;

        public QuestRewardRoller(QuestRewardPools pools, IRunSeedProvider seedProvider)
        {
            _pools = pools ?? throw new ArgumentNullException(nameof(pools));
            _seedProvider = seedProvider ?? throw new ArgumentNullException(nameof(seedProvider));
        }

        public bool TryRoll(QuestRewardCore reward, string contextKey, out QuestRewardRollResult result)
        {
            result = default;
            if (reward == null)
            {
                return false;
            }

            var random = new Random(LootSeed.Derive(_seedProvider.RunSeed, ContextKeyPrefix + contextKey));
            switch (reward.PayloadKind)
            {
                case QuestRewardPayloadKind.Artifact:
                    return TryRollArtifact(reward, random, out result);
                case QuestRewardPayloadKind.PartBlank:
                    return TryRollBlank(reward, random, out result);
                default:
                    return false;
            }
        }

        private bool TryRollArtifact(QuestRewardCore reward, Random random, out QuestRewardRollResult result)
        {
            result = default;
            var family = FilterArtifactsByFamily(reward.BelongingId);
            if (family.Count == 0)
            {
                return false;
            }

            var candidates = NearestTierSubset(family, reward.Tier);
            var picked = candidates[random.Next(candidates.Count)];
            result = new QuestRewardRollResult(QuestRewardPayloadKind.Artifact, picked.Id);
            return true;
        }

        private bool TryRollBlank(QuestRewardCore reward, Random random, out QuestRewardRollResult result)
        {
            result = default;
            var candidates = FilterBlanksByRace(reward.BelongingId);
            if (candidates.Count == 0)
            {
                return false;
            }

            var picked = candidates[random.Next(candidates.Count)];
            result = new QuestRewardRollResult(QuestRewardPayloadKind.PartBlank, picked.Id);
            return true;
        }

        /// <summary>An empty declared belonging draws from the whole pool; a declared one is a hard
        /// filter (colour honesty outranks payout robustness — an impossible family is an authoring
        /// error the granter logs, not something to silently substitute).</summary>
        private List<RewardArtifactOption> FilterArtifactsByFamily(string familyId)
        {
            var all = _pools.Artifacts;
            var filtered = new List<RewardArtifactOption>();
            for (int i = 0; i < all.Count; i++)
            {
                if (string.IsNullOrEmpty(familyId)
                    || string.Equals(all[i].FamilyId, familyId, StringComparison.Ordinal))
                {
                    filtered.Add(all[i]);
                }
            }

            return filtered;
        }

        private List<RewardBlankOption> FilterBlanksByRace(string raceId)
        {
            var all = _pools.Blanks;
            var filtered = new List<RewardBlankOption>();
            for (int i = 0; i < all.Count; i++)
            {
                if (string.IsNullOrEmpty(raceId)
                    || string.Equals(all[i].RaceId, raceId, StringComparison.Ordinal))
                {
                    filtered.Add(all[i]);
                }
            }

            return filtered;
        }

        /// <summary>The entries whose tier is closest to the declared tier (exact match when it
        /// exists). Keeps the promised potency honest while degrading gracefully on sparse pools.</summary>
        private static List<RewardArtifactOption> NearestTierSubset(List<RewardArtifactOption> pool, int tier)
        {
            int bestDistance = int.MaxValue;
            for (int i = 0; i < pool.Count; i++)
            {
                int distance = Math.Abs(pool[i].Tier - tier);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                }
            }

            var subset = new List<RewardArtifactOption>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (Math.Abs(pool[i].Tier - tier) == bestDistance)
                {
                    subset.Add(pool[i]);
                }
            }

            return subset;
        }
    }
}
