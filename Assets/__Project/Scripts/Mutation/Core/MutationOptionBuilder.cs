using System;
using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Default <see cref="IMutationOptionBuilder"/>: scores every candidate part against the stage's
    /// cumulative feed tally and returns the top-N. The score is the dot product of the tally vector
    /// and the part's archetype-affinity vector, scaled by a rarity multiplier that only ramps up
    /// once enough archetype points have been accumulated (see <see cref="MutationScoringParameters"/>).
    /// Parts already equipped, and parts with no affinity to anything fed (score zero), are excluded.
    /// Ordering is descending by score with an ordinal part-id tie-break, so the choice is
    /// deterministic for repeatable tests.
    /// </summary>
    public sealed class MutationOptionBuilder : IMutationOptionBuilder
    {
        public IReadOnlyList<MutationOption> Build(
            IReadOnlyDictionary<string, float> tallyTotals,
            IReadOnlyList<MutationCandidatePart> candidates,
            ISet<string> equippedPartIds,
            int maxOptions,
            MutationScoringParameters scoring)
        {
            if (candidates == null || tallyTotals == null || maxOptions <= 0)
            {
                return Array.Empty<MutationOption>();
            }

            var totalPoints = SumValues(tallyTotals);

            var scored = new List<ScoredCandidate>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.PartId))
                {
                    continue;
                }

                if (equippedPartIds != null && equippedPartIds.Contains(candidate.PartId))
                {
                    continue;
                }

                var score = Score(candidate, tallyTotals, totalPoints, scoring);
                if (score <= 0f)
                {
                    continue;
                }

                scored.Add(new ScoredCandidate(candidate, score));
            }

            scored.Sort(CompareByScoreThenId);

            var take = Math.Min(maxOptions, scored.Count);
            var chosen = new List<MutationOption>(take);
            for (int i = 0; i < take; i++)
            {
                var candidate = scored[i].Part;
                chosen.Add(new MutationOption(
                    candidate.SlotId,
                    candidate.PartId,
                    candidate.DominantArchetypeId,
                    candidate.DisplayName));
            }

            return chosen;
        }

        private static float Score(
            MutationCandidatePart candidate,
            IReadOnlyDictionary<string, float> tallyTotals,
            float totalPoints,
            MutationScoringParameters scoring)
        {
            // Affinity match: dot product of the fed tally and this part's affinity vector. Iterate
            // the (usually smaller) affinity map and look each archetype up in the tally. A part with
            // no affinity to anything fed scores zero and is dropped - you mutate toward what you fed.
            var affinityScore = 0f;
            foreach (var entry in candidate.Affinity)
            {
                if (tallyTotals.TryGetValue(entry.Key, out var fed))
                {
                    affinityScore += fed * entry.Value;
                }
            }

            if (affinityScore <= 0f)
            {
                return 0f;
            }

            // Rarity multiplier: rarer parts (higher tier) are favoured, but only as accumulated
            // points approach the tier's unlock threshold - so a slightly-lower-affinity rare part
            // overtakes a common one once the player has fed enough. Common parts (tier 0) get a
            // multiplier of 1 (no boost, no gate).
            var multiplier = 1f;
            if (candidate.RarityTier > 0 && scoring.RarityWeight > 0f)
            {
                var unlock = 1f;
                if (scoring.RarityUnlockPointsPerTier > 0f)
                {
                    var required = candidate.RarityTier * scoring.RarityUnlockPointsPerTier;
                    unlock = Clamp01(totalPoints / required);
                }

                multiplier = 1f + scoring.RarityWeight * candidate.RarityTier * unlock;
            }

            return affinityScore * multiplier;
        }

        private static float SumValues(IReadOnlyDictionary<string, float> totals)
        {
            var sum = 0f;
            foreach (var value in totals.Values)
            {
                sum += value;
            }

            return sum;
        }

        private static int CompareByScoreThenId(ScoredCandidate a, ScoredCandidate b)
        {
            var byScore = b.Score.CompareTo(a.Score); // descending
            if (byScore != 0)
            {
                return byScore;
            }

            return string.CompareOrdinal(a.Part.PartId, b.Part.PartId);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private readonly struct ScoredCandidate
        {
            public readonly MutationCandidatePart Part;
            public readonly float Score;

            public ScoredCandidate(MutationCandidatePart part, float score)
            {
                Part = part;
                Score = score;
            }
        }
    }
}
