namespace Hub.Core
{
    /// <summary>
    /// Deterministic line pick for the Hub's cauldron voice (O1): a stable FNV-1a hash over
    /// (moment, race, salt) indexes the pool, so the same staging state always speaks the same
    /// line (no in-run RNG stream is touched) while the salt — the upcoming run's index — cycles
    /// the pool across runs.
    /// </summary>
    public static class CauldronVoiceSelector
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        public static bool TrySelect(CauldronVoiceLines lines, CauldronVoiceMoment moment,
            string raceId, int salt, out string line)
        {
            line = null;
            var pool = lines?.PoolFor(moment, raceId);
            if (pool == null || pool.Count == 0)
            {
                return false;
            }

            uint hash = Hash(moment, raceId, salt);
            line = pool[(int)(hash % (uint)pool.Count)];
            return true;
        }

        private static uint Hash(CauldronVoiceMoment moment, string raceId, int salt)
        {
            // FNV-1a: stable across processes (string.GetHashCode is not, by design).
            uint hash = FnvOffsetBasis;
            hash = Step(hash, (uint)moment);
            hash = Step(hash, unchecked((uint)salt));
            if (!string.IsNullOrEmpty(raceId))
            {
                foreach (char c in raceId)
                {
                    hash = Step(hash, c);
                }
            }

            return hash;
        }

        private static uint Step(uint hash, uint value)
        {
            unchecked
            {
                hash ^= value;
                return hash * FnvPrime;
            }
        }
    }
}
