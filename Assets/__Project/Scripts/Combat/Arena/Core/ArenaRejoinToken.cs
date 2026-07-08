using Loot.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The rejoin credential: derived from the match seed + seat, never stored or granted.
    /// Every participant can compute every seat's token from the setup it already holds — which
    /// is exactly what host migration needs (any promoted peer can validate rejoins). Blocks
    /// non-participants (they never saw the seed); it does NOT block a malicious participant
    /// impersonating another disconnected seat — a documented limitation until X3 brings real
    /// per-player auth.
    /// </summary>
    public static class ArenaRejoinToken
    {
        public static ulong For(int matchSeed, int playerId)
        {
            // Two independent 32-bit derivations packed into one 64-bit token, so guessing it
            // is not a single-int brute force.
            uint lo = unchecked((uint)LootSeed.Derive(matchSeed, $"arena-rejoin-lo:{playerId}"));
            uint hi = unchecked((uint)LootSeed.Derive(matchSeed, $"arena-rejoin-hi:{playerId}"));
            return ((ulong)hi << 32) | lo;
        }
    }
}
