using System;
using System.Collections.Generic;

namespace Loot.Core
{
    /// <summary>One rollable artifact reward candidate: its definition id, authored tier, and
    /// reward-family id (empty = family-less; only matched when the declaration is unconstrained).</summary>
    public sealed class RewardArtifactOption
    {
        public string Id { get; }
        public int Tier { get; }
        public string FamilyId { get; }

        public RewardArtifactOption(string id, int tier, string familyId)
        {
            Id = id ?? string.Empty;
            Tier = tier;
            FamilyId = familyId ?? string.Empty;
        }
    }

    /// <summary>One rollable Part-Blank reward candidate: its definition id and race tag
    /// (empty = kindless).</summary>
    public sealed class RewardBlankOption
    {
        public string Id { get; }
        public string RaceId { get; }

        public RewardBlankOption(string id, string raceId)
        {
            Id = id ?? string.Empty;
            RaceId = raceId ?? string.Empty;
        }
    }

    /// <summary>
    /// The eligible pools the quest-reward roll draws from (P1-5), built once at install time from
    /// the authored artifact/blank catalogs — pure records only, so the roller stays UnityEngine-free.
    /// </summary>
    public sealed class QuestRewardPools
    {
        public IReadOnlyList<RewardArtifactOption> Artifacts { get; }
        public IReadOnlyList<RewardBlankOption> Blanks { get; }

        public QuestRewardPools(IReadOnlyList<RewardArtifactOption> artifacts,
            IReadOnlyList<RewardBlankOption> blanks)
        {
            Artifacts = artifacts ?? Array.Empty<RewardArtifactOption>();
            Blanks = blanks ?? Array.Empty<RewardBlankOption>();
        }
    }
}
