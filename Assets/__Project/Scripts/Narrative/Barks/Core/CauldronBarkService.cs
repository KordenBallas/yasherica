using System;
using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Barks.Core
{
    /// <summary>
    /// Default <see cref="ICauldronBarkService"/> (P1-10). Selection is deterministic under the run
    /// seed: a stable FNV-1a hash over (slot, lean, run seed) anchors each slot's sequence, and a
    /// per-slot fire counter walks the pool from that anchor — so replays speak identically, and
    /// consecutive barks from one slot never repeat while the pool has more than one line. The lean
    /// is re-read from the path facts at every fire (the voice tracks the run live).
    /// </summary>
    public sealed class CauldronBarkService : ICauldronBarkService
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        private readonly CauldronBarkLines _lines;
        private readonly IFactStore _facts;
        private readonly int _runSeed;
        private readonly Dictionary<CauldronBarkSlot, int> _fireCounts =
            new Dictionary<CauldronBarkSlot, int>();

        public event Action<string> OnBark;

        public CauldronBarkService(CauldronBarkLines lines, IFactStore facts, int runSeed)
        {
            _lines = lines ?? CauldronBarkLines.Empty;
            _facts = facts;
            _runSeed = runSeed;
        }

        public void Bark(CauldronBarkSlot slot)
        {
            var lean = PathLeanEvaluator.Evaluate(_facts);
            var pool = _lines.PoolFor(slot, lean);
            if (pool.Count == 0)
            {
                return; // quiet moment, never an error
            }

            _fireCounts.TryGetValue(slot, out var fired);
            _fireCounts[slot] = fired + 1;

            uint anchor = Hash(slot, lean, _runSeed);
            var line = pool[(int)((anchor + (uint)fired) % (uint)pool.Count)];
            OnBark?.Invoke(line);
        }

        public bool IsDarkBelonging(string belongingId) => _lines.IsDarkBelonging(belongingId);

        private static uint Hash(CauldronBarkSlot slot, BarkLean lean, int seed)
        {
            // FNV-1a: stable across processes (string/enum GetHashCode is not, by design).
            uint hash = FnvOffsetBasis;
            hash = Step(hash, (uint)slot);
            hash = Step(hash, (uint)lean);
            hash = Step(hash, unchecked((uint)seed));
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
