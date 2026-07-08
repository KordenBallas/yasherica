using System;
using System.Collections.Generic;
using MetaProgression.Core;
using Narrative.Director.Core;

namespace Hub.Core
{
    /// <summary>
    /// Forms the Hub's starting-part offer (O1, owner call 2026-07-06): a deterministic seeded draw
    /// of N cards from the tasted pool. Greedy variety per draw — an unseen race beats an unseen
    /// slot beats a "flavored" candidate (race-tagged or ability-bearing beats plain scrap) — with
    /// the seeded stream breaking the remaining ties, so the offer reads as a direction choice, not
    /// a vending machine, and the same (pool, seed) always deals the same cards.
    ///
    /// Track R (FR8–FR10): the tie-break may be WEIGHTED by the pursued direction —
    /// <c>w = pow(drawWeight · (1 + biasStrength · directionScore), dilutionExponent)</c> — with
    /// every weight clamped so no candidate's tie-pick share can reach the configured ceiling
    /// (&lt; 100%, the never-guarantee invariant). When the weights come out uniform (no direction,
    /// bias off, neutral profile) the pick is byte-identical to the legacy unbiased draw.
    /// </summary>
    public sealed class StartingPartSelector
    {
        private const int UnseenRaceScore = 4;
        private const int UnseenSlotScore = 2;
        private const int FlavoredScore = 1;

        // Resolution of the uniform variate carved from the seeded integer stream for weighted picks.
        private const int WeightedPickResolution = 1000000;
        private const float WeightEpsilon = 1e-6f;
        private const int CeilingClampMaxPasses = 16;

        public IReadOnlyList<StartingPartCandidate> Draw(
            IReadOnlyList<StartingPartCandidate> pool, int count, int seed)
        {
            return Draw(pool, count, seed, null, null);
        }

        public IReadOnlyList<StartingPartCandidate> Draw(
            IReadOnlyList<StartingPartCandidate> pool, int count, int seed,
            DirectionProfile direction, MetaProgressionSettings settings)
        {
            var picks = new List<StartingPartCandidate>();
            if (pool == null || count <= 0)
            {
                return picks;
            }

            // Sorted copy: the draw must not depend on the caller's pool order.
            var remaining = new List<StartingPartCandidate>(pool.Count);
            foreach (var candidate in pool)
            {
                if (candidate != null && !string.IsNullOrEmpty(candidate.PartId))
                {
                    remaining.Add(candidate);
                }
            }

            remaining.Sort((a, b) => string.CompareOrdinal(a.PartId, b.PartId));

            var random = new DeterministicRandom(unchecked((ulong)seed));
            var offeredRaces = new HashSet<string>(StringComparer.Ordinal);
            var offeredSlots = new HashSet<string>(StringComparer.Ordinal);
            var tieSet = new List<int>();

            while (picks.Count < count && remaining.Count > 0)
            {
                int bestScore = int.MinValue;
                tieSet.Clear();
                for (int i = 0; i < remaining.Count; i++)
                {
                    int score = Score(remaining[i], offeredRaces, offeredSlots);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        tieSet.Clear();
                    }

                    if (score == bestScore)
                    {
                        tieSet.Add(i);
                    }
                }

                int pickedIndex = PickFromTieSet(remaining, tieSet, random, direction, settings);
                var picked = remaining[pickedIndex];
                remaining.RemoveAt(pickedIndex);
                picks.Add(picked);

                if (!string.IsNullOrEmpty(picked.RaceId))
                {
                    offeredRaces.Add(picked.RaceId);
                }

                if (!string.IsNullOrEmpty(picked.SlotId))
                {
                    offeredSlots.Add(picked.SlotId);
                }
            }

            ApplyDirectionFloor(picks, remaining, direction, settings);
            return picks;
        }

        private static int Score(StartingPartCandidate candidate,
            HashSet<string> offeredRaces, HashSet<string> offeredSlots)
        {
            int score = 0;
            if (!string.IsNullOrEmpty(candidate.RaceId) && !offeredRaces.Contains(candidate.RaceId))
            {
                score += UnseenRaceScore;
            }

            if (!string.IsNullOrEmpty(candidate.SlotId) && !offeredSlots.Contains(candidate.SlotId))
            {
                score += UnseenSlotScore;
            }

            if (!string.IsNullOrEmpty(candidate.RaceId) || candidate.HasActiveAbility)
            {
                score += FlavoredScore;
            }

            return score;
        }

        /// <summary>
        /// The tie-break. Uniform weights take the legacy path (one <c>NextInt(tieSet.Count)</c> —
        /// the zero-diff guarantee); differing weights consume one <c>NextInt(resolution)</c> and
        /// pick by cumulative weight after the never-guarantee ceiling clamp.
        /// </summary>
        private static int PickFromTieSet(
            List<StartingPartCandidate> remaining, List<int> tieSet, DeterministicRandom random,
            DirectionProfile direction, MetaProgressionSettings settings)
        {
            if (tieSet.Count == 1 || settings == null)
            {
                return tieSet[random.NextInt(tieSet.Count)];
            }

            var weights = new double[tieSet.Count];
            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < tieSet.Count; i++)
            {
                var candidate = remaining[tieSet[i]];
                float match = direction?.ScoreFor(candidate.RaceId, candidate.TraitIds) ?? 0f;
                double weight = candidate.DrawWeight * (1.0 + settings.BiasStrength * match);
                weight = Math.Pow(weight, settings.DilutionExponent);
                weights[i] = weight;
                if (weight < min) min = weight;
                if (weight > max) max = weight;
            }

            if (max - min < WeightEpsilon)
            {
                return tieSet[random.NextInt(tieSet.Count)];
            }

            ClampToCeiling(weights, settings.BiasCeiling);

            double total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                total += weights[i];
            }

            double target = (random.NextInt(WeightedPickResolution) / (double)WeightedPickResolution) * total;
            double cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (target < cumulative)
                {
                    return tieSet[i];
                }
            }

            return tieSet[tieSet.Count - 1];
        }

        /// <summary>
        /// FR9: caps every weight at <c>ceiling/(1−ceiling) · Σ(others)</c> so no single pick
        /// probability can reach the ceiling. Iterated to a fixpoint because clamping one weight
        /// shrinks the others' bounds (weights only ever decrease, so this converges).
        /// </summary>
        private static void ClampToCeiling(double[] weights, float ceiling)
        {
            if (ceiling <= 0f || ceiling >= 1f || weights.Length < 2)
            {
                return;
            }

            double ratio = ceiling / (1.0 - ceiling);
            for (int pass = 0; pass < CeilingClampMaxPasses; pass++)
            {
                bool changed = false;
                double total = 0;
                for (int i = 0; i < weights.Length; i++)
                {
                    total += weights[i];
                }

                for (int i = 0; i < weights.Length; i++)
                {
                    double bound = ratio * (total - weights[i]);
                    if (weights[i] > bound)
                    {
                        weights[i] = bound;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// The dig-shape floor rule (config-gated, default off): when the offer holds no card
        /// matching the pursued direction and the leftover pool does, the weakest-matching pick is
        /// swapped for the best-matching leftover (ties by ordinal part id) — the direction "raises
        /// the floor", still never a guarantee of a specific piece.
        /// </summary>
        private static void ApplyDirectionFloor(
            List<StartingPartCandidate> picks, List<StartingPartCandidate> remaining,
            DirectionProfile direction, MetaProgressionSettings settings)
        {
            if (settings == null || !settings.ReserveDirectionSlot
                || direction == null || direction.IsNeutral
                || picks.Count == 0 || remaining.Count == 0)
            {
                return;
            }

            for (int i = 0; i < picks.Count; i++)
            {
                if (direction.ScoreFor(picks[i].RaceId, picks[i].TraitIds) > 0f)
                {
                    return; // The offer already carries the direction.
                }
            }

            StartingPartCandidate best = null;
            float bestScore = 0f;
            foreach (var candidate in remaining)
            {
                float score = direction.ScoreFor(candidate.RaceId, candidate.TraitIds);
                if (score > bestScore
                    || (score == bestScore && score > 0f
                        && string.CompareOrdinal(candidate.PartId, best.PartId) < 0))
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            if (best != null)
            {
                picks[picks.Count - 1] = best;
            }
        }
    }
}
