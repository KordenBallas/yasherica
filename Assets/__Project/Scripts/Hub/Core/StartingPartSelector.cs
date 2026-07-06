using System;
using System.Collections.Generic;
using Narrative.Director.Core;

namespace Hub.Core
{
    /// <summary>
    /// Forms the Hub's starting-part offer (O1, owner call 2026-07-06): a deterministic seeded draw
    /// of N cards from the tasted pool. Greedy variety per draw — an unseen race beats an unseen
    /// slot beats a "flavored" candidate (race-tagged or ability-bearing beats plain scrap) — with
    /// the seeded stream breaking the remaining ties, so the offer reads as a direction choice, not
    /// a vending machine, and the same (pool, seed) always deals the same cards.
    /// </summary>
    public sealed class StartingPartSelector
    {
        private const int UnseenRaceScore = 4;
        private const int UnseenSlotScore = 2;
        private const int FlavoredScore = 1;

        public IReadOnlyList<StartingPartCandidate> Draw(
            IReadOnlyList<StartingPartCandidate> pool, int count, int seed)
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

                int pickedIndex = tieSet[random.NextInt(tieSet.Count)];
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
    }
}
