namespace Narrative.Threads.Core
{
    /// <summary>Why a thread left the Live state with <see cref="ThreadState.Failed"/> (FR4/FR5).</summary>
    public enum ThreadRetirementReason
    {
        None = 0,

        /// <summary>Ephemeral thread un-advanced past its authored lifespan (silent timeout).</summary>
        Expired = 1,

        /// <summary>A written fact contradicted the thread's premise — the player made it
        /// impossible. Applies to arc threads too.</summary>
        Conflict = 2,

        /// <summary>The player killed the thread's character (the Monster verb, P1-7) — the arc is
        /// forfeit as the in-fiction price of the corpse loot. A state change + indicator, no
        /// closure beat, like every retirement.</summary>
        Foreclosed = 3
    }
}
