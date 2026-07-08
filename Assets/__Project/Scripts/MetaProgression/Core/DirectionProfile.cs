using System.Collections.Generic;

namespace MetaProgression.Core
{
    /// <summary>
    /// The pursued direction (meta-progression FR8): normalized 0..1 scores per race marker and per
    /// artifact function-trait, tallied from the last few runs' ledger. A candidate's match is the
    /// STRONGER of its two axis reads (a token squarely on either axis is "in the direction").
    /// Immutable; <see cref="Neutral"/> (empty ledger / no meta bindings) scores everything 0, so
    /// the dig draw degrades to the unbiased legacy pick.
    /// </summary>
    public sealed class DirectionProfile
    {
        public static readonly DirectionProfile Neutral = new DirectionProfile(null, null);

        private readonly Dictionary<string, float> _raceScores;
        private readonly Dictionary<string, float> _traitScores;

        public bool IsNeutral => _raceScores.Count == 0 && _traitScores.Count == 0;

        /// <summary>Raw weights in; normalized jointly so the strongest signal across both axes = 1.</summary>
        public DirectionProfile(
            IReadOnlyDictionary<string, float> raceWeights,
            IReadOnlyDictionary<string, float> traitWeights)
        {
            _raceScores = new Dictionary<string, float>(System.StringComparer.Ordinal);
            _traitScores = new Dictionary<string, float>(System.StringComparer.Ordinal);

            float max = 0f;
            max = MaxWeight(raceWeights, max);
            max = MaxWeight(traitWeights, max);
            if (max <= 0f)
            {
                return;
            }

            CopyNormalized(raceWeights, _raceScores, max);
            CopyNormalized(traitWeights, _traitScores, max);
        }

        /// <summary>0..1 match of a candidate: the stronger of its race lean and its best trait lean.</summary>
        public float ScoreFor(string raceId, IReadOnlyList<string> traitIds)
        {
            float score = 0f;
            if (!string.IsNullOrEmpty(raceId) && _raceScores.TryGetValue(raceId, out float raceScore))
            {
                score = raceScore;
            }

            if (traitIds != null)
            {
                for (int i = 0; i < traitIds.Count; i++)
                {
                    if (traitIds[i] != null && _traitScores.TryGetValue(traitIds[i], out float traitScore)
                        && traitScore > score)
                    {
                        score = traitScore;
                    }
                }
            }

            return score;
        }

        private static float MaxWeight(IReadOnlyDictionary<string, float> weights, float max)
        {
            if (weights == null)
            {
                return max;
            }

            foreach (var pair in weights)
            {
                if (pair.Value > max)
                {
                    max = pair.Value;
                }
            }

            return max;
        }

        private static void CopyNormalized(
            IReadOnlyDictionary<string, float> source, Dictionary<string, float> target, float max)
        {
            if (source == null)
            {
                return;
            }

            foreach (var pair in source)
            {
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0f)
                {
                    target[pair.Key] = pair.Value / max;
                }
            }
        }
    }
}
