namespace LevelGeneration.Journey
{
    /// <summary>
    /// Run-scoped journey rule changes (heat-ascension FR8) as a neutral-by-default record, so the
    /// journey never references the Heat system: the Area installer projects the run's pact onto it.
    /// The tier lift is applied at the ONE fact-write site (<see cref="BiomeStretchDirector"/>), so
    /// every consumer — story tier-bands, monster pools, the restore replay — reads one number.
    /// </summary>
    public sealed class JourneyRuleModifiers
    {
        /// <summary>Today's rules — no modifier active.</summary>
        public static readonly JourneyRuleModifiers Neutral = new JourneyRuleModifiers(0);

        /// <summary>Added to every stretch's authored escalation tier (the raised creature floor —
        /// a D19 pool shift: WHICH creatures, never scaled ones).</summary>
        public int EscalationTierLift { get; }

        public JourneyRuleModifiers(int escalationTierLift)
        {
            EscalationTierLift = escalationTierLift < 0 ? 0 : escalationTierLift;
        }
    }
}
