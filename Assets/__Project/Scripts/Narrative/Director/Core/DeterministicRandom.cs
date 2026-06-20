namespace Narrative.Director.Core
{
    /// <summary>
    /// SplitMix64 counter-based PRNG (B2). Unlike <see cref="System.Random"/> its state is a single
    /// <see cref="ulong"/> that can be serialized and restored exactly, so procedural draws after a
    /// reload reproduce the uninterrupted sequence. Pure C#.
    /// </summary>
    public sealed class DeterministicRandom : IRandomSource
    {
        private ulong _state;

        public DeterministicRandom(ulong seed)
        {
            _state = seed;
        }

        public ulong State
        {
            get => _state;
            set => _state = value;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1)
            {
                return 0;
            }

            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        private ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
