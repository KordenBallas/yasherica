using System;
using System.Collections.Generic;
using Inventory.Core;

namespace Mutation.Core
{
    /// <summary>
    /// Scores every non-equipped authored part of the blank's slot against the
    /// socketed reagents: the profiles are combined through the SAME emergent
    /// fusion grammar as the cauldron (sockets interact - an emergent third
    /// property counts toward affinity), then each part scores its trait-affinity
    /// overlap with the resulting target, boosted by a rarity gate that unlocks
    /// with the target's tier. Deterministic: score desc, ordinal part-id
    /// tie-break. No zero-score filter (raw-only is weak, never dead).
    /// </summary>
    public class BlankVariantBuilder : IBlankVariantBuilder
    {
        private readonly EmergentFusionCalculator _fusionCalculator;
        private readonly TraitFusionRuleSet _fusionRules;
        private readonly FusionSettings _fusionSettings;

        public BlankVariantBuilder(
            EmergentFusionCalculator fusionCalculator,
            TraitFusionRuleSet fusionRules,
            FusionSettings fusionSettings)
        {
            _fusionCalculator = fusionCalculator ?? throw new ArgumentNullException(nameof(fusionCalculator));
            _fusionRules = fusionRules ?? throw new ArgumentNullException(nameof(fusionRules));
            _fusionSettings = fusionSettings ?? throw new ArgumentNullException(nameof(fusionSettings));
        }

        public IReadOnlyList<MutationOption> Build(
            PartBlankData blank,
            IReadOnlyList<ArtifactTraitProfile> socketedProfiles,
            IReadOnlyList<MutationCandidatePart> candidateParts,
            IReadOnlyCollection<string> equippedPartIds,
            int maxOptions,
            VariantScoringParameters scoring)
        {
            if (blank == null || candidateParts == null || maxOptions < 1)
            {
                return Array.Empty<MutationOption>();
            }

            var target = _fusionCalculator.ComputeTarget(socketedProfiles, _fusionRules, _fusionSettings);

            var scored = new List<ScoredCandidate>();
            foreach (var candidate in candidateParts)
            {
                if (candidate == null || string.IsNullOrEmpty(candidate.PartId))
                {
                    continue;
                }

                if (!string.Equals(candidate.SlotId, blank.SlotId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (equippedPartIds != null && Contains(equippedPartIds, candidate.PartId))
                {
                    continue;
                }

                scored.Add(new ScoredCandidate(candidate, Score(candidate, target, scoring)));
            }

            scored.Sort(CompareByScoreThenId);

            int count = maxOptions < scored.Count ? maxOptions : scored.Count;
            var options = new List<MutationOption>(count);
            for (int i = 0; i < count; i++)
            {
                var candidate = scored[i].Candidate;
                // The card's tint is the blank's species marker - the blank, not
                // the part, carries the passport.
                options.Add(new MutationOption(
                    blank.SlotId,
                    candidate.PartId,
                    blank.SpeciesArchetypeId,
                    candidate.DisplayName));
            }

            return options;
        }

        private static float Score(
            MutationCandidatePart candidate,
            ArtifactTraitProfile target,
            VariantScoringParameters scoring)
        {
            float overlap = 0f;
            foreach (var affinity in candidate.TraitAffinity)
            {
                if (target.Has(affinity.Key))
                {
                    overlap += affinity.Value;
                }
            }

            return overlap * RarityMultiplier(candidate.RarityTier, target.Tier, scoring);
        }

        private static float RarityMultiplier(int rarityTier, int targetTier, VariantScoringParameters scoring)
        {
            if (rarityTier <= 0 || scoring.RarityWeight <= 0f)
            {
                return 1f;
            }

            float unlockTier = rarityTier * scoring.TierUnlockPerRarityTier;
            float unlock = unlockTier <= 0f ? 1f : targetTier / unlockTier;
            if (unlock > 1f)
            {
                unlock = 1f;
            }

            return 1f + scoring.RarityWeight * rarityTier * unlock;
        }

        private static int CompareByScoreThenId(ScoredCandidate a, ScoredCandidate b)
        {
            int byScore = b.Score.CompareTo(a.Score);
            return byScore != 0
                ? byScore
                : string.CompareOrdinal(a.Candidate.PartId, b.Candidate.PartId);
        }

        private static bool Contains(IReadOnlyCollection<string> ids, string id)
        {
            foreach (var candidate in ids)
            {
                if (string.Equals(candidate, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct ScoredCandidate
        {
            public MutationCandidatePart Candidate { get; }
            public float Score { get; }

            public ScoredCandidate(MutationCandidatePart candidate, float score)
            {
                Candidate = candidate;
                Score = score;
            }
        }
    }
}
