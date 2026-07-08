namespace Combat.Core
{
    /// <summary>
    /// Run-scoped combat rule changes (heat-ascension FR8/FR10) as a neutral-by-default record, so
    /// combat never references the Heat system: the Area installer projects the run's pact onto it;
    /// the Arena and any un-modified fight simply run on <see cref="Neutral"/>.
    /// </summary>
    public sealed class CombatRuleModifiers
    {
        /// <summary>Today's rules — no modifier active.</summary>
        public static readonly CombatRuleModifiers Neutral = new CombatRuleModifiers(false);

        /// <summary>Enemies resolve before the player EVERY round, not just an enemy-led opening (D2 stays for round 1 semantics).</summary>
        public bool EnemiesAlwaysLead { get; }

        public CombatRuleModifiers(bool enemiesAlwaysLead)
        {
            EnemiesAlwaysLead = enemiesAlwaysLead;
        }
    }
}
