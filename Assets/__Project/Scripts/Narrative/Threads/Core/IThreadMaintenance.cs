using Narrative.Facts.Core;

namespace Narrative.Threads.Core
{
    /// <summary>
    /// The thread lifecycle tick (D13/D14). Runs once at the start of each window planning pass —
    /// never off <c>OnFactChanged</c> — so retirement is a deterministic step in the planning
    /// sequence and replay does not depend on UI/event timing (FR12), and a premise fact transiently
    /// flipped mid-dialogue cannot kill a thread mid-beat.
    /// </summary>
    public interface IThreadMaintenance
    {
        /// <summary>Per live thread, in ledger order: fold the advance flag into the expiry clock,
        /// then resolution conditions → premise conflict → expiry (conflict outranks expiry — the
        /// player-caused ending wins over the clock). Draws no randomness.</summary>
        void Tick(int windowIndex, IFactStore facts);
    }
}
