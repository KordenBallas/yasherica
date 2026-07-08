using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The X2 volley budget: a seat banks one scheduling round per accepted EMPTY commit and
    /// spends the bank when a volley fires — so a forged 3-ability volley out of nowhere is
    /// rejected. An UPPER bound only: schedule and end-turn are wire-indistinguishable (both are
    /// empty commitments by design — the queue stays hidden), so passing rounds also bank
    /// credits; the queue-size cap does the rest. Reset on rejoin/resync (the transferred state
    /// has an empty queue).
    /// </summary>
    public class ArenaQueueCreditLedger
    {
        private readonly Dictionary<int, int> _credits = new Dictionary<int, int>();

        public int CreditsOf(int playerId)
        {
            return _credits.TryGetValue(playerId, out var credits) ? credits : 0;
        }

        /// <summary>An accepted empty commit banks one scheduling round.</summary>
        public void BankSchedulingRound(int playerId)
        {
            _credits[playerId] = CreditsOf(playerId) + 1;
        }

        /// <summary>
        /// An accepted volley spends the bank (the queue clears at normalization). A move leaves
        /// it untouched — the hidden queue survives a repositioning round.
        /// </summary>
        public void SpendBank(int playerId)
        {
            _credits[playerId] = 0;
        }

        /// <summary>A rejoined/resynced seat starts from an empty queue.</summary>
        public void Reset(int playerId)
        {
            _credits[playerId] = 0;
        }
    }
}
