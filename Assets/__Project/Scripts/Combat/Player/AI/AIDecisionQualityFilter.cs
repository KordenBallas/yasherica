using System;
using System.Collections.Generic;
using System.Linq;

namespace Combat.Player.AI
{
    /// <summary>
    /// The difficulty dial: turns a scored candidate list into one pick through the only
    /// randomness in the pipeline (mistake roll → per-candidate score noise → top-N pick).
    /// With perfect tuning (noise 0, topN 1, mistake 0) it degenerates to argmax. The draw
    /// order is fixed and every draw always happens on its path, so the same seed and the
    /// same candidate list always produce the same pick.
    /// </summary>
    public sealed class AIDecisionQualityFilter
    {
        private readonly Random _random;

        public AIDecisionQualityFilter(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public AICandidate Pick(IReadOnlyList<AIScoredCandidate> scored, AITuning tuning)
        {
            if (scored == null || scored.Count == 0)
                throw new ArgumentException("Cannot pick from an empty candidate list.", nameof(scored));

            if (_random.NextDouble() < tuning.MistakeChance)
                return scored[_random.Next(scored.Count)].Candidate;

            var noisy = new List<AIScoredCandidate>(scored.Count);
            foreach (var entry in scored)
            {
                // Always draw, even at noise 0, so the random stream length only depends on
                // the candidate count — a state-deterministic quantity.
                float noise = (float)(_random.NextDouble() * 2.0 - 1.0) * tuning.ScoreNoise;
                noisy.Add(new AIScoredCandidate(entry.Candidate, entry.Score + noise));
            }

            // OrderByDescending is a stable sort: equal scores keep enumeration order.
            var ranked = noisy.OrderByDescending(entry => entry.Score).ToList();
            int topCount = Math.Min(tuning.PickFromTopN, ranked.Count);
            return ranked[_random.Next(topCount)].Candidate;
        }
    }
}
