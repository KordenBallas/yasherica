using System;

namespace Loot.Core
{
    /// <summary>
    /// Derives per-context random seeds from the run seed using FNV-1a hashing.
    /// Each loot roll gets its own seed so results are deterministic within a run,
    /// vary across runs, and are immune to roll-order changes and to the global
    /// UnityEngine.Random stream.
    /// </summary>
    public static class LootSeed
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        public static int Derive(int runSeed, string contextKey)
        {
            if (string.IsNullOrEmpty(contextKey))
            {
                throw new ArgumentException("Context key must not be null or empty.", nameof(contextKey));
            }

            var hash = FnvOffsetBasis;
            hash = HashByte(hash, (byte)runSeed);
            hash = HashByte(hash, (byte)(runSeed >> 8));
            hash = HashByte(hash, (byte)(runSeed >> 16));
            hash = HashByte(hash, (byte)(runSeed >> 24));

            for (int i = 0; i < contextKey.Length; i++)
            {
                var character = contextKey[i];
                hash = HashByte(hash, (byte)character);
                hash = HashByte(hash, (byte)(character >> 8));
            }

            return unchecked((int)hash);
        }

        private static uint HashByte(uint hash, byte value)
        {
            return (hash ^ value) * FnvPrime;
        }
    }
}
