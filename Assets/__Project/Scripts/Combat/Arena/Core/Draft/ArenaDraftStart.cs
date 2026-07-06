using System;
using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Host → all: the draft handshake. Unlike the match setup (seed-only), the composed board
    /// travels in full — composition is host-authoritative (the host alone holds every
    /// participant's tasted catalog), clients never recompute it. The slot loadout rides along
    /// so replicas can never disagree with the host about completion.
    /// </summary>
    public class ArenaDraftStart
    {
        public float PickTimerSeconds { get; }
        public IReadOnlyList<string> SlotLoadout { get; }
        public IReadOnlyList<ArenaDraftBoardEntry> Board { get; }

        public ArenaDraftStart(
            float pickTimerSeconds,
            IReadOnlyList<string> slotLoadout,
            IReadOnlyList<ArenaDraftBoardEntry> board)
        {
            PickTimerSeconds = pickTimerSeconds;
            SlotLoadout = slotLoadout ?? Array.Empty<string>();
            Board = board ?? Array.Empty<ArenaDraftBoardEntry>();
        }
    }
}
